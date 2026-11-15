namespace BanesaHub.Domain.Tenants;

public sealed class OccupancyPeriod
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid UnitId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; set; }
}
