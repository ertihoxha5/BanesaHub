namespace BanesaHub.Domain.Organizations;

public sealed class Membership
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public required MembershipRole Role { get; init; }
    public bool IsActive { get; init; } = true;
}

public enum MembershipRole
{
    Owner = 1,
    PropertyManager = 2,
    FinanceStaff = 3,
    MaintenanceStaff = 4,
    Tenant = 5
}
