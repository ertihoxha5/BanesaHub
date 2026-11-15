using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations/{organizationId:guid}/leases")]
public sealed class LeasesController(ILeaseLifecycle lifecycle) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDraft(Guid organizationId, DraftRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
        {
            return Unauthorized();
        }

        var id = await lifecycle.CreateDraftAsync(organizationId, actorId,
            new LeaseDraft(request.UnitId, request.TenantId, request.StartDate, request.EndDate,
                request.MonthlyRent!.Value, request.Deposit), ct);
        return id is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPost("{leaseId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid organizationId, Guid leaseId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
        {
            return Unauthorized();
        }

        return await lifecycle.ActivateAsync(organizationId, actorId, leaseId, ct) switch
        {
            LeaseActivation.Activated => NoContent(),
            LeaseActivation.NotFound => NotFound(),
            LeaseActivation.Overlap => Problem(statusCode: 409, title: "Njësia ka një kontratë aktive që mbivendoset."),
            _ => Problem(statusCode: 409, title: "Kjo kontratë nuk mund të aktivizohet në gjendjen aktuale.")
        };
    }

    public sealed record DraftRequest(Guid UnitId, Guid TenantId, DateOnly StartDate, DateOnly? EndDate,
        [Required] decimal? MonthlyRent, decimal? Deposit) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UnitId == Guid.Empty || TenantId == Guid.Empty)
            {
                yield return new ValidationResult("Zgjidhni njësinë dhe qiramarrësin.", [nameof(UnitId), nameof(TenantId)]);
            }

            if (StartDate == default || EndDate < StartDate)
            {
                yield return new ValidationResult("Datat e kontratës nuk janë të vlefshme.", [nameof(StartDate), nameof(EndDate)]);
            }

            if (MonthlyRent is decimal rent && !ValidAmount(rent))
            {
                yield return new ValidationResult("Qiraja mujore nuk është e vlefshme.", [nameof(MonthlyRent)]);
            }

            if (Deposit is decimal deposit && !ValidAmount(deposit))
            {
                yield return new ValidationResult("Depozita nuk është e vlefshme.", [nameof(Deposit)]);
            }
        }

        private static bool ValidAmount(decimal value) =>
            value is >= 0 and <= 9999999999999999.99m && decimal.Round(value, 2) == value;
    }
}
