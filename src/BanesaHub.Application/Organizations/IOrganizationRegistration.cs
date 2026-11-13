namespace BanesaHub.Application.Organizations;

public interface IOrganizationRegistration
{
    Task<Guid?> CreateAsync(Guid ownerId, string name, string slug, CancellationToken cancellationToken);
}
