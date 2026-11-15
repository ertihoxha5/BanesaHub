namespace BanesaHub.Domain.Properties;

public sealed class Property
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
}

public enum PropertyStatus
{
    Draft = 1,
    Active = 2,
    Archived = 3
}
