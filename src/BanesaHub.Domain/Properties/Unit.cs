namespace BanesaHub.Domain.Properties;

public sealed class Unit
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid BuildingId { get; init; }
    public required string Number { get; set; }
    public decimal? Area { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Vacant;
    public bool IsArchived { get; set; }
}

public enum UnitStatus
{
    Vacant = 1,
    Occupied = 2,
    Unavailable = 3
}
