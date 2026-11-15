using BanesaHub.Domain.Properties;

namespace BanesaHub.Application.Properties;

public sealed record PropertyDetails(Guid Id, string Name, string? Address, string? Category, PropertyStatus Status);
public sealed record PropertyInput(string Name, string? Address, string? Category);

public interface IPropertyInventory
{
    Task<IReadOnlyList<PropertyDetails>?> ListAsync(Guid organizationId, Guid actorId, int offset, int limit, CancellationToken ct, string? search = null, PropertyStatus? status = null);
    Task<PropertyDetails?> FindAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct);
    Task<PropertyDetails?> CreateAsync(Guid organizationId, Guid actorId, PropertyInput input, CancellationToken ct);
    Task<bool> UpdateAsync(Guid organizationId, Guid actorId, Guid id, PropertyInput input, CancellationToken ct);
    Task<bool> ArchiveAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct);
}
