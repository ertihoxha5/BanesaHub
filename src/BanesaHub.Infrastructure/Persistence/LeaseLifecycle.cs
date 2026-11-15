using System.Data;
using BanesaHub.Application.Tenants;
using BanesaHub.Domain.Properties;
using BanesaHub.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class LeaseLifecycle(BanesaHubDbContext database, TimeProvider clock) : ILeaseLifecycle
{
    public async Task<Guid?> CreateDraftAsync(Guid organizationId, Guid actorId, LeaseDraft draft, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct) ||
            !await EligibleUnits(organizationId).AnyAsync(unit => unit.Id == draft.UnitId, ct) ||
            !await TenantExists(organizationId, draft.TenantId, ct))
        {
            return null;
        }

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = draft.TenantId,
            UnitId = draft.UnitId
        };
        database.Leases.Add(lease);
        database.LeaseVersions.Add(new LeaseVersion
        {
            OrganizationId = organizationId,
            LeaseId = lease.Id,
            VersionNumber = 1,
            StartDate = draft.StartDate,
            EndDate = draft.EndDate,
            EffectiveFrom = draft.StartDate,
            MonthlyRent = draft.MonthlyRent,
            Deposit = draft.Deposit,
            RecordedAtUtc = clock.GetUtcNow()
        });
        await database.SaveChangesAsync(ct);
        return lease.Id;
    }

    public Task<LeaseOutcome> ActivateAsync(Guid organizationId, Guid actorId, Guid leaseId, CancellationToken ct) =>
        ChangeAsync(organizationId, actorId, leaseId, async (lease, terms) =>
        {
            if (lease.Status == LeaseStatus.Active)
            {
                return LeaseOutcome.Applied;
            }

            if (lease.Status != LeaseStatus.Draft ||
                !await EligibleUnits(organizationId).AnyAsync(item => item.Id == lease.UnitId, ct) ||
                !await TenantExists(organizationId, lease.TenantId, ct))
            {
                return LeaseOutcome.InvalidState;
            }

            var existingTerms = from existing in database.Leases
                                where existing.OrganizationId == organizationId && existing.UnitId == lease.UnitId &&
                                      existing.Id != lease.Id &&
                                      (existing.Status == LeaseStatus.Active || existing.Status == LeaseStatus.Ended)
                                from version in database.LeaseVersions
                                    .Where(item => item.OrganizationId == organizationId && item.LeaseId == existing.Id)
                                    .OrderByDescending(item => item.VersionNumber).Take(1)
                                select new { version.StartDate, EndDate = existing.EndedOn ?? version.EndDate };
            var endDate = terms.EndDate;
            var startDate = terms.StartDate;
            if (await existingTerms.AnyAsync(item =>
                    (endDate == null || item.StartDate <= endDate) &&
                    (item.EndDate == null || item.EndDate >= startDate), ct))
            {
                return LeaseOutcome.Overlap;
            }

            lease.Status = LeaseStatus.Active;
            return LeaseOutcome.Applied;
        }, ct);

    public Task<LeaseOutcome> AmendRentAsync(Guid organizationId, Guid actorId, Guid leaseId,
        RentAmendment amendment, CancellationToken ct) =>
        ChangeAsync(organizationId, actorId, leaseId, (lease, terms) =>
        {
            if (lease.Status != LeaseStatus.Active)
            {
                return Task.FromResult(LeaseOutcome.InvalidState);
            }

            if (terms.VersionNumber != amendment.ExpectedVersion)
            {
                return Task.FromResult(LeaseOutcome.StaleVersion);
            }

            if (amendment.EffectiveFrom <= terms.EffectiveFrom || amendment.EffectiveFrom > terms.EndDate)
            {
                return Task.FromResult(LeaseOutcome.InvalidDate);
            }

            database.LeaseVersions.Add(new LeaseVersion
            {
                OrganizationId = organizationId,
                LeaseId = leaseId,
                VersionNumber = checked(terms.VersionNumber + 1),
                StartDate = terms.StartDate,
                EndDate = terms.EndDate,
                EffectiveFrom = amendment.EffectiveFrom,
                MonthlyRent = amendment.MonthlyRent,
                Deposit = terms.Deposit,
                RecordedAtUtc = clock.GetUtcNow()
            });
            return Task.FromResult(LeaseOutcome.Applied);
        }, ct);

    public Task<LeaseOutcome> EndAsync(Guid organizationId, Guid actorId, Guid leaseId, DateOnly endedOn, CancellationToken ct) =>
        ChangeAsync(organizationId, actorId, leaseId, (lease, terms) =>
        {
            if (lease.Status == LeaseStatus.Ended && lease.EndedOn == endedOn)
            {
                return Task.FromResult(LeaseOutcome.Applied);
            }

            if (lease.Status != LeaseStatus.Active)
            {
                return Task.FromResult(LeaseOutcome.InvalidState);
            }

            var localTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.GetUtcNow(), "Europe/Belgrade");
            if (endedOn < terms.StartDate || endedOn > terms.EndDate || endedOn > DateOnly.FromDateTime(localTime.Date))
            {
                return Task.FromResult(LeaseOutcome.InvalidDate);
            }

            lease.EndedOn = endedOn;
            lease.Status = LeaseStatus.Ended;
            return Task.FromResult(LeaseOutcome.Applied);
        }, ct);

    private async Task<LeaseOutcome> ChangeAsync(Guid organizationId, Guid actorId, Guid leaseId,
        Func<Lease, LeaseVersion, Task<LeaseOutcome>> change, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return LeaseOutcome.NotFound;
        }

        var scopedLease = database.Leases.Where(lease => lease.OrganizationId == organizationId && lease.Id == leaseId);
        var unitId = await scopedLease.Select(lease => (Guid?)lease.UnitId).SingleOrDefaultAsync(ct);
        if (unitId is null)
        {
            return LeaseOutcome.NotFound;
        }

        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Serialize lease changes for a unit before reading current terms or state.
        var lockedUnit = await database.Units.FromSqlInterpolated($"""
            SELECT * FROM dbo.Units WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = {unitId.Value} AND OrganizationId = {organizationId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (lockedUnit is null || !await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return LeaseOutcome.NotFound;
        }

        var lease = await scopedLease.SingleOrDefaultAsync(ct);
        if (lease is null)
        {
            return LeaseOutcome.NotFound;
        }

        var terms = await database.LeaseVersions.Where(item => item.OrganizationId == organizationId && item.LeaseId == leaseId)
            .OrderByDescending(item => item.VersionNumber).FirstOrDefaultAsync(ct);
        if (terms is null)
        {
            return LeaseOutcome.InvalidState;
        }

        var result = await change(lease, terms);
        if (result == LeaseOutcome.Applied)
        {
            await database.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        return result;
    }

    private IQueryable<Unit> EligibleUnits(Guid organizationId) =>
        from unit in database.Units
        join building in database.Buildings on new { unit.OrganizationId, Id = unit.BuildingId }
            equals new { building.OrganizationId, building.Id }
        join property in database.Properties on new { building.OrganizationId, Id = building.PropertyId }
            equals new { property.OrganizationId, property.Id }
        where unit.OrganizationId == organizationId && !unit.IsArchived && unit.Status != UnitStatus.Unavailable &&
              !building.IsArchived && property.Status != PropertyStatus.Archived
        select unit;

    private Task<bool> TenantExists(Guid organizationId, Guid tenantId, CancellationToken ct) =>
        database.Tenants.AnyAsync(tenant => tenant.OrganizationId == organizationId &&
            tenant.Id == tenantId && !tenant.IsArchived, ct);
}
