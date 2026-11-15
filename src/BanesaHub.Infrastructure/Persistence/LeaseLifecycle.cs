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

    public async Task<LeaseActivation> ActivateAsync(Guid organizationId, Guid actorId, Guid leaseId, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return LeaseActivation.NotFound;
        }

        var scopedLeases = database.Leases.Where(lease => lease.OrganizationId == organizationId);
        var unitId = await scopedLeases.Where(lease => lease.Id == leaseId)
            .Select(lease => (Guid?)lease.UnitId).SingleOrDefaultAsync(ct);
        if (unitId is null)
        {
            return LeaseActivation.NotFound;
        }

        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Lock the unit before reading lease state. All date-changing lease writes must take this lock first.
        var lockedUnit = await database.Units.FromSqlInterpolated($"""
            SELECT * FROM dbo.Units WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = {unitId.Value} AND OrganizationId = {organizationId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (lockedUnit is null || !await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return LeaseActivation.NotFound;
        }

        var lease = await scopedLeases.SingleOrDefaultAsync(item => item.Id == leaseId, ct);
        if (lease is null)
        {
            return LeaseActivation.NotFound;
        }

        if (lease.Status == LeaseStatus.Active)
        {
            return LeaseActivation.Activated;
        }

        var terms = await database.LeaseVersions.Where(item => item.OrganizationId == organizationId && item.LeaseId == leaseId)
            .OrderByDescending(item => item.VersionNumber).FirstOrDefaultAsync(ct);
        if (lease.Status != LeaseStatus.Draft || terms is null ||
            !await EligibleUnits(organizationId).AnyAsync(item => item.Id == lease.UnitId, ct) ||
            !await TenantExists(organizationId, lease.TenantId, ct))
        {
            return LeaseActivation.InvalidState;
        }

        var activeTerms = from active in scopedLeases
                          where active.Id != leaseId && active.UnitId == unitId.Value && active.Status == LeaseStatus.Active
                          from version in database.LeaseVersions
                              .Where(item => item.OrganizationId == organizationId && item.LeaseId == active.Id)
                              .OrderByDescending(item => item.VersionNumber).Take(1)
                          select version;
        var endDate = terms.EndDate;
        var startDate = terms.StartDate;
        if (await activeTerms.AnyAsync(item =>
                (endDate == null || item.StartDate <= endDate) &&
                (item.EndDate == null || item.EndDate >= startDate), ct))
        {
            return LeaseActivation.Overlap;
        }

        lease.Status = LeaseStatus.Active;
        await database.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return LeaseActivation.Activated;
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
