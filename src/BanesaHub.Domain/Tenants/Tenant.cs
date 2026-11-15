namespace BanesaHub.Domain.Tenants;

public sealed class Tenant
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsArchived { get; set; }
}
