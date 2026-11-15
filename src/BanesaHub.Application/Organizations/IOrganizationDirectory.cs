namespace BanesaHub.Application.Organizations;

public interface IOrganizationDirectory
{
    Task<OrganizationDetails?> FindForMemberAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken);
}

public sealed record OrganizationDetails(Guid Id, string Name, string Slug);
