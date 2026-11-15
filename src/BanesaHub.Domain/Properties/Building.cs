namespace BanesaHub.Domain.Properties;

public sealed class Building
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid PropertyId { get; init; }
    public required string Name { get; set; }
    public int? FloorCount { get; set; }
    public bool IsArchived { get; set; }
}
