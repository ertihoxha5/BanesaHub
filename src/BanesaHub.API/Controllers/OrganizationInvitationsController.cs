using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Organizations;
using BanesaHub.Domain.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/organizations")]
public sealed class OrganizationInvitationsController(IOrganizationInvitations invitations) : ControllerBase
{
    [HttpPost("{organizationId:guid}/invitations")]
    public async Task<IActionResult> Issue(Guid organizationId, IssueRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        if (request.Role is not (MembershipRole.PropertyManager or MembershipRole.FinanceStaff
            or MembershipRole.MaintenanceStaff or MembershipRole.Tenant))
        {
            return Problem(statusCode: 400, title: "Roli i zgjedhur nuk lejohet për ftesa.");
        }

        var invitation = await invitations.IssueAsync(userId, organizationId, request.Email, request.Role, cancellationToken);
        return invitation is null ? NotFound() : StatusCode(StatusCodes.Status201Created, invitation);
    }

    [HttpPost("invitations/accept")]
    public async Task<IActionResult> Accept(AcceptRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        return await invitations.AcceptAsync(userId, request.Token, cancellationToken)
            ? NoContent()
            : Problem(statusCode: 400, title: "Ftesa nuk mund të pranohet.");
    }

    public sealed record IssueRequest([Required, EmailAddress, StringLength(256)] string Email, MembershipRole Role);
    public sealed record AcceptRequest([Required, RegularExpression("[A-F0-9]{64}")] string Token);
}
