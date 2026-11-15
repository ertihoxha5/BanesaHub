namespace BanesaHub.Domain.Tenants;

public sealed class LeaseVersion
{
    public Guid OrganizationId { get; init; }
    public Guid LeaseId { get; init; }
    public int VersionNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public decimal MonthlyRent { get; init; }
    public decimal? Deposit { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
}
