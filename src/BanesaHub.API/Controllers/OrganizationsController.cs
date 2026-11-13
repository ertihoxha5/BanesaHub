using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(IOrganizationRegistration registration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerId))
        {
            return Unauthorized();
        }

        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (name.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(slug, @"\A[a-z0-9]+(?:-[a-z0-9]+)*\z"))
        {
            return Problem(statusCode: 400, title: "Emri ose identifikuesi i organizatës nuk është i vlefshëm.");
        }

        var id = await registration.CreateAsync(ownerId, name, slug, cancellationToken);
        return id is null
            ? Problem(statusCode: 409, title: "Ky identifikues i organizatës është në përdorim.")
            : StatusCode(StatusCodes.Status201Created, new { id, name, slug });
    }

    public sealed record CreateOrganizationRequest(
        [Required, StringLength(200)] string Name,
        [Required, StringLength(100)] string Slug);
}
