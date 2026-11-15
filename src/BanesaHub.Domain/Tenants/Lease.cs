namespace BanesaHub.Domain.Tenants;

public sealed class Lease
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UnitId { get; init; }
    public Guid TenantId { get; init; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Draft;
}

public enum LeaseStatus
{
    Draft = 1,
    Active = 2,
    Ended = 3,
    Cancelled = 4
}
