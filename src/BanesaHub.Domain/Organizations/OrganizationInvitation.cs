namespace BanesaHub.Domain.Organizations;

public sealed class OrganizationInvitation
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public required string NormalizedEmail { get; init; }
    public required string TokenHash { get; init; }
    public required MembershipRole Role { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; set; }
}
