using BanesaHub.Domain.Properties;

namespace BanesaHub.Application.Properties;

public sealed record UnitDetails(Guid Id, Guid BuildingId, string Number, decimal? Area, UnitStatus Status);
public sealed record UnitInput(string Number, decimal? Area, UnitStatus Status);

public enum UnitWriteResult
{
    Saved,
    NotFound,
    DuplicateNumber
}

public interface IUnitInventory
{
    Task<IReadOnlyList<UnitDetails>?> ListAsync(Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, int offset, int limit, CancellationToken ct);
    Task<UnitDetails?> FindAsync(Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, CancellationToken ct);
    Task<(UnitWriteResult Result, UnitDetails? Unit)> CreateAsync(Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, UnitInput input, CancellationToken ct);
    Task<UnitWriteResult> UpdateAsync(Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, UnitInput input, CancellationToken ct);
    Task<bool> ArchiveAsync(Guid organizationId, Guid propertyId, Guid buildingId, Guid actorId, Guid id, CancellationToken ct);
}
