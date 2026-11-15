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

        return FromOutcome(await lifecycle.ActivateAsync(organizationId, actorId, leaseId, ct));
    }

    [HttpPost("{leaseId:guid}/amend-rent")]
    public async Task<IActionResult> AmendRent(Guid organizationId, Guid leaseId, RentAmendmentRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
        {
            return Unauthorized();
        }

        var amendment = new RentAmendment(request.ExpectedVersion, request.EffectiveFrom, request.MonthlyRent!.Value);
        return FromOutcome(await lifecycle.AmendRentAsync(organizationId, actorId, leaseId, amendment, ct));
    }

    [HttpPost("{leaseId:guid}/end")]
    public async Task<IActionResult> End(Guid organizationId, Guid leaseId, EndRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
        {
            return Unauthorized();
        }

        return FromOutcome(await lifecycle.EndAsync(organizationId, actorId, leaseId, request.EndedOn!.Value, ct));
    }

    private IActionResult FromOutcome(LeaseOutcome outcome) => outcome switch
    {
        LeaseOutcome.Applied => NoContent(),
        LeaseOutcome.NotFound => NotFound(),
        LeaseOutcome.Overlap => Problem(statusCode: 409, title: "Njësia ka një kontratë që mbivendoset."),
        LeaseOutcome.InvalidDate => Problem(statusCode: 409, title: "Data nuk përputhet me kushtet e kontratës."),
        LeaseOutcome.StaleVersion => Problem(statusCode: 409, title: "Versioni ka ndryshuar. Kontrolloni kushtet e fundit të kontratës."),
        _ => Problem(statusCode: 409, title: "Gjendja e kontratës nuk e lejon këtë veprim.")
    };

    public sealed record EndRequest([Required] DateOnly? EndedOn);

    public sealed record RentAmendmentRequest([Range(1, int.MaxValue)] int ExpectedVersion,
        DateOnly EffectiveFrom, [Required] decimal? MonthlyRent) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EffectiveFrom == default)
            {
                yield return new ValidationResult("Shkruani datën e hyrjes në fuqi.", [nameof(EffectiveFrom)]);
            }

            if (MonthlyRent is decimal rent && !ValidAmount(rent))
            {
                yield return new ValidationResult("Qiraja mujore nuk është e vlefshme.", [nameof(MonthlyRent)]);
            }
        }
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
    }

    private static bool ValidAmount(decimal value) =>
            value is >= 0 and <= 9999999999999999.99m && decimal.Round(value, 2) == value;
}
