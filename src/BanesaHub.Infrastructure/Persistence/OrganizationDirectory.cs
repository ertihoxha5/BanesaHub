using BanesaHub.Application.Organizations;
using BanesaHub.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class OrganizationDirectory(BanesaHubDbContext database) : IOrganizationDirectory
{
    public Task<OrganizationDetails?> FindForMemberAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        database.Organizations
            .Where(organization => organization.Id == organizationId && organization.IsActive)
            .Where(organization => ActiveMemberships(database, userId)
                .Any(membership => membership.OrganizationId == organization.Id))
            .Select(organization => new OrganizationDetails(organization.Id, organization.Name, organization.Slug))
            .SingleOrDefaultAsync(cancellationToken);

    internal static IQueryable<Membership> ActiveMemberships(BanesaHubDbContext database, Guid userId) =>
        database.Memberships.Where(membership => membership.UserId == userId && membership.IsActive);
}
