namespace BanesaHub.Application.Tenants;

public sealed record TenantDetails(Guid Id, string FullName, string? Email, string? Phone);
public sealed record TenantInput(string FullName, string? Email, string? Phone);

public interface ITenantContacts
{
    Task<IReadOnlyList<TenantDetails>?> ListAsync(Guid organizationId, Guid actorId, int offset, int limit,
        string? search, CancellationToken ct);
    Task<TenantDetails?> FindAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct);
    Task<TenantDetails?> CreateAsync(Guid organizationId, Guid actorId, TenantInput input, CancellationToken ct);
    Task<bool> UpdateAsync(Guid organizationId, Guid actorId, Guid id, TenantInput input, CancellationToken ct);
    Task<bool> ArchiveAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct);
}
