namespace BanesaHub.Domain.Organizations;

public sealed class Organization
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public bool IsActive { get; init; } = true;
}
