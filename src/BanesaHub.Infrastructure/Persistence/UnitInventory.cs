using System.Linq.Expressions;
using BanesaHub.Application.Properties;
using BanesaHub.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class UnitInventory(BanesaHubDbContext database) : IUnitInventory
{
    private static readonly Expression<Func<Unit, UnitDetails>> ToDetails =
        unit => new UnitDetails(unit.Id, unit.BuildingId, unit.Number, unit.Area, unit.Status);

    public async Task<IReadOnlyList<UnitDetails>?> ListAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, int offset, int limit, CancellationToken ct)
    {
        if (!await CanManageBuilding(organizationId, propertyId, buildingId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId, buildingId).OrderBy(item => item.Number).ThenBy(item => item.Id)
            .Skip(offset).Take(limit).Select(ToDetails).ToListAsync(ct);
    }

    public async Task<UnitDetails?> FindAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, CancellationToken ct)
    {
        if (!await CanManageBuilding(organizationId, propertyId, buildingId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId, buildingId).Where(item => item.Id == id)
            .Select(ToDetails).SingleOrDefaultAsync(ct);
    }

    public async Task<(UnitWriteResult Result, UnitDetails? Unit)> CreateAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, UnitInput input, CancellationToken ct)
    {
        if (!await CanManageBuilding(organizationId, propertyId, buildingId, actorId, ct))
        {
            return (UnitWriteResult.NotFound, null);
        }

        if (await NumberExists(organizationId, buildingId, input.Number, null, ct))
        {
            return (UnitWriteResult.DuplicateNumber, null);
        }

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BuildingId = buildingId,
            Number = input.Number,
            Area = input.Area,
            Status = input.Status
        };
        database.Units.Add(unit);
        var result = await SaveAsync(ct);
        return (result, result == UnitWriteResult.Saved
            ? new UnitDetails(unit.Id, unit.BuildingId, unit.Number, unit.Area, unit.Status)
            : null);
    }

    public async Task<UnitWriteResult> UpdateAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, UnitInput input, CancellationToken ct)
    {
        var unit = await FindEditable(organizationId, propertyId, buildingId, actorId, id, ct);
        if (unit is null)
        {
            return UnitWriteResult.NotFound;
        }

        if (await NumberExists(organizationId, buildingId, input.Number, id, ct))
        {
            return UnitWriteResult.DuplicateNumber;
        }

        unit.Number = input.Number;
        unit.Area = input.Area;
        unit.Status = input.Status;
        return await SaveAsync(ct);
    }

    public async Task<bool> ArchiveAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, CancellationToken ct)
    {
        var unit = await FindEditable(organizationId, propertyId, buildingId, actorId, id, ct);
        if (unit is null)
        {
            return false;
        }

        unit.IsArchived = true;
        await database.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Unit> Visible(Guid organizationId, Guid buildingId) =>
        database.Units.Where(item => item.OrganizationId == organizationId &&
            item.BuildingId == buildingId && !item.IsArchived);

    private async Task<Unit?> FindEditable(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, CancellationToken ct) =>
        await CanManageBuilding(organizationId, propertyId, buildingId, actorId, ct)
            ? await Visible(organizationId, buildingId).SingleOrDefaultAsync(item => item.Id == id, ct)
            : null;

    private async Task<bool> CanManageBuilding(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, CancellationToken ct) =>
        await BuildingInventory.CanManageProperty(database, organizationId, propertyId, actorId, ct) &&
        await database.Buildings.AnyAsync(item => item.OrganizationId == organizationId &&
            item.PropertyId == propertyId && item.Id == buildingId && !item.IsArchived, ct);

    private Task<bool> NumberExists(Guid organizationId, Guid buildingId, string number, Guid? excludedId, CancellationToken ct) =>
        database.Units.AnyAsync(item => item.OrganizationId == organizationId &&
            item.BuildingId == buildingId && item.Number == number && item.Id != excludedId, ct);

    private async Task<UnitWriteResult> SaveAsync(CancellationToken ct)
    {
        try
        {
            await database.SaveChangesAsync(ct);
            return UnitWriteResult.Saved;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return UnitWriteResult.DuplicateNumber;
        }
    }
}
