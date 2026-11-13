using BanesaHub.Application.Organizations;
using BanesaHub.Domain.Organizations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class OrganizationRegistration(BanesaHubDbContext database) : IOrganizationRegistration
{
    public async Task<Guid?> CreateAsync(
        Guid ownerId, string name, string slug, CancellationToken cancellationToken)
    {
        if (await database.Organizations.AnyAsync(item => item.Slug == slug, cancellationToken))
        {
            return null;
        }

        var id = Guid.NewGuid();
        database.Organizations.Add(new Organization { Id = id, Name = name, Slug = slug });
        database.Memberships.Add(new Membership
        {
            OrganizationId = id,
            UserId = ownerId,
            Role = MembershipRole.Owner
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return id;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (await database.Organizations.AnyAsync(item => item.Slug == slug, cancellationToken))
            {
                return null;
            }

            throw;
        }
    }
}
