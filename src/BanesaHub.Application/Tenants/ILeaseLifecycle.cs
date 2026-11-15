namespace BanesaHub.Application.Tenants;

public sealed record LeaseDraft(Guid UnitId, Guid TenantId, DateOnly StartDate, DateOnly? EndDate,
    decimal MonthlyRent, decimal? Deposit);

public sealed record RentAmendment(int ExpectedVersion, DateOnly EffectiveFrom, decimal MonthlyRent);

public enum LeaseOutcome
{
    Applied,
    NotFound,
    InvalidState,
    Overlap,
    InvalidDate,
    StaleVersion
}

public interface ILeaseLifecycle
{
    Task<Guid?> CreateDraftAsync(Guid organizationId, Guid actorId, LeaseDraft draft, CancellationToken ct);
    Task<LeaseOutcome> ActivateAsync(Guid organizationId, Guid actorId, Guid leaseId, CancellationToken ct);
    Task<LeaseOutcome> AmendRentAsync(Guid organizationId, Guid actorId, Guid leaseId, RentAmendment amendment, CancellationToken ct);
    Task<LeaseOutcome> EndAsync(Guid organizationId, Guid actorId, Guid leaseId, DateOnly endedOn, CancellationToken ct);
}
