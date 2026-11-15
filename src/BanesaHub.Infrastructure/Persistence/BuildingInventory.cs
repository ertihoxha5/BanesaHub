using System.Linq.Expressions;
using BanesaHub.Application.Properties;
using BanesaHub.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class BuildingInventory(BanesaHubDbContext database) : IBuildingInventory
{
    private static readonly Expression<Func<Building, BuildingDetails>> ToDetails =
        building => new BuildingDetails(building.Id, building.PropertyId, building.Name, building.FloorCount);

    public async Task<IReadOnlyList<BuildingDetails>?> ListAsync(
        Guid organizationId, Guid propertyId, Guid actorId, int offset, int limit, CancellationToken ct)
    {
        if (!await CanManageProperty(organizationId, propertyId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId, propertyId).OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Skip(offset).Take(limit).Select(ToDetails).ToListAsync(ct);
    }

    public async Task<BuildingDetails?> FindAsync(
        Guid organizationId, Guid propertyId, Guid actorId, Guid id, CancellationToken ct)
    {
        if (!await CanManageProperty(organizationId, propertyId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId, propertyId).Where(item => item.Id == id)
            .Select(ToDetails).SingleOrDefaultAsync(ct);
    }

    public async Task<BuildingDetails?> CreateAsync(
        Guid organizationId, Guid propertyId, Guid actorId, BuildingInput input, CancellationToken ct)
    {
        if (!await CanManageProperty(organizationId, propertyId, actorId, ct))
        {
            return null;
        }

        var building = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PropertyId = propertyId,
            Name = input.Name,
            FloorCount = input.FloorCount
        };
        database.Buildings.Add(building);
        await database.SaveChangesAsync(ct);
        return new BuildingDetails(building.Id, building.PropertyId, building.Name, building.FloorCount);
    }

    public async Task<bool> UpdateAsync(
        Guid organizationId, Guid propertyId, Guid actorId, Guid id, BuildingInput input, CancellationToken ct)
    {
        var building = await FindEditable(organizationId, propertyId, actorId, id, ct);
        if (building is null)
        {
            return false;
        }

        building.Name = input.Name;
        building.FloorCount = input.FloorCount;
        await database.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ArchiveAsync(
        Guid organizationId, Guid propertyId, Guid actorId, Guid id, CancellationToken ct)
    {
        var building = await FindEditable(organizationId, propertyId, actorId, id, ct);
        if (building is null)
        {
            return false;
        }

        building.IsArchived = true;
        await database.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Building> Visible(Guid organizationId, Guid propertyId) =>
        database.Buildings.Where(item => item.OrganizationId == organizationId &&
            item.PropertyId == propertyId && !item.IsArchived);

    private async Task<Building?> FindEditable(
        Guid organizationId, Guid propertyId, Guid actorId, Guid id, CancellationToken ct) =>
        await CanManageProperty(organizationId, propertyId, actorId, ct)
            ? await Visible(organizationId, propertyId).SingleOrDefaultAsync(item => item.Id == id, ct)
            : null;

    private async Task<bool> CanManageProperty(Guid organizationId, Guid propertyId, Guid actorId, CancellationToken ct) =>
        await PropertyInventory.CanManage(database, organizationId, actorId, ct) &&
        await database.Properties.AnyAsync(item => item.OrganizationId == organizationId &&
            item.Id == propertyId && item.Status != PropertyStatus.Archived, ct);
}
