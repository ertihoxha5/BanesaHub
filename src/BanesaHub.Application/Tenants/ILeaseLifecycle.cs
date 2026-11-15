namespace BanesaHub.Application.Tenants;

public sealed record LeaseDraft(Guid UnitId, Guid TenantId, DateOnly StartDate, DateOnly? EndDate,
    decimal MonthlyRent, decimal? Deposit);

public enum LeaseActivation
{
    Activated,
    NotFound,
    InvalidState,
    Overlap
}

public interface ILeaseLifecycle
{
    Task<Guid?> CreateDraftAsync(Guid organizationId, Guid actorId, LeaseDraft draft, CancellationToken ct);
    Task<LeaseActivation> ActivateAsync(Guid organizationId, Guid actorId, Guid leaseId, CancellationToken ct);
}
