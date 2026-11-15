namespace BanesaHub.Application.Properties;

public sealed record BuildingDetails(Guid Id, Guid PropertyId, string Name, int? FloorCount);
public sealed record BuildingInput(string Name, int? FloorCount);

public interface IBuildingInventory
{
    Task<IReadOnlyList<BuildingDetails>?> ListAsync(Guid organizationId, Guid propertyId, Guid actorId, int offset, int limit, CancellationToken ct);
    Task<BuildingDetails?> FindAsync(Guid organizationId, Guid propertyId, Guid actorId, Guid id, CancellationToken ct);
    Task<BuildingDetails?> CreateAsync(Guid organizationId, Guid propertyId, Guid actorId, BuildingInput input, CancellationToken ct);
    Task<bool> UpdateAsync(Guid organizationId, Guid propertyId, Guid actorId, Guid id, BuildingInput input, CancellationToken ct);
    Task<bool> ArchiveAsync(Guid organizationId, Guid propertyId, Guid actorId, Guid id, CancellationToken ct);
}
