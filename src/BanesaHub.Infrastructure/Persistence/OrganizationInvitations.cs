using System.Security.Cryptography;
using System.Text;
using BanesaHub.Application.Organizations;
using BanesaHub.Domain.Organizations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class OrganizationInvitations(BanesaHubDbContext database, TimeProvider clock)
    : IOrganizationInvitations
{
    public async Task<IssuedInvitation?> IssueAsync(Guid ownerId, Guid organizationId, string email,
        MembershipRole role, CancellationToken cancellationToken)
    {
        if (role is not (MembershipRole.PropertyManager or MembershipRole.FinanceStaff
            or MembershipRole.MaintenanceStaff or MembershipRole.Tenant))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        var canInvite = await OrganizationDirectory.ActiveMemberships(database, ownerId).AnyAsync(member =>
            member.OrganizationId == organizationId && member.Role == MembershipRole.Owner &&
            database.Organizations.Any(org => org.Id == organizationId && org.IsActive),
            cancellationToken);
        if (!canInvite)
        {
            return null;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiry = clock.GetUtcNow().AddHours(48);
        database.Set<OrganizationInvitation>().Add(new OrganizationInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NormalizedEmail = email.Trim().Normalize().ToUpperInvariant(),
            Role = role,
            TokenHash = Hash(token),
            ExpiresAt = expiry
        });
        await database.SaveChangesAsync(cancellationToken);
        return new IssuedInvitation(token, expiry);
    }

    public async Task<bool> AcceptAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(token);
        var invitation = await database.Set<OrganizationInvitation>().SingleOrDefaultAsync(item =>
            item.TokenHash == hash && item.AcceptedAt == null && item.ExpiresAt > now &&
            database.Organizations.Any(org => org.Id == item.OrganizationId && org.IsActive) &&
            database.Users.Any(user => user.Id == userId && user.NormalizedEmail == item.NormalizedEmail),
            cancellationToken);

        if (invitation is null || await database.Memberships.AnyAsync(member =>
            member.OrganizationId == invitation.OrganizationId && member.UserId == userId,
            cancellationToken))
        {
            return false;
        }

        invitation.AcceptedAt = now;
        database.AuditEvents.Add(new AuditEvent
        {
            ActorId = userId,
            OrganizationId = invitation.OrganizationId,
            Action = $"MembershipRoleGranted.{invitation.Role}",
            At = now
        });
        database.Memberships.Add(new Membership
        {
            OrganizationId = invitation.OrganizationId,
            UserId = userId,
            Role = invitation.Role
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
