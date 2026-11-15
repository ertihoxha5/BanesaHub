using BanesaHub.Domain.Organizations;

namespace BanesaHub.Application.Organizations;

public interface IOrganizationInvitations
{
    Task<IssuedInvitation?> IssueAsync(Guid ownerId, Guid organizationId, string email,
        MembershipRole role, CancellationToken cancellationToken);

    Task<bool> AcceptAsync(Guid userId, string token, CancellationToken cancellationToken);
}

public sealed record IssuedInvitation(string Token, DateTimeOffset ExpiresAt);
