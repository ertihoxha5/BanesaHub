using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Properties;
using BanesaHub.Domain.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations/{organizationId:guid}/properties/{propertyId:guid}/buildings/{buildingId:guid}/units")]
public sealed class UnitsController(IUnitInventory inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, Guid propertyId, Guid buildingId, CancellationToken ct,
        [Range(0, int.MaxValue)] int offset = 0, [Range(1, 100)] int limit = 25)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var units = await inventory.ListAsync(organizationId, propertyId, buildingId, actorId, offset, limit, ct);
        return units is null ? NotFound() : Ok(units);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid propertyId, Guid buildingId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var unit = await inventory.FindAsync(organizationId, propertyId, buildingId, actorId, id, ct);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, Guid propertyId, Guid buildingId, UnitRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var (result, unit) = await inventory.CreateAsync(organizationId, propertyId, buildingId, actorId, request.ToInput(), ct);
        return result switch
        {
            UnitWriteResult.DuplicateNumber => DuplicateNumber(),
            UnitWriteResult.Saved when unit is not null =>
                CreatedAtAction(nameof(Get), new { organizationId, propertyId, buildingId, unit.Id }, unit),
            _ => NotFound()
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid propertyId, Guid buildingId, Guid id, UnitRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var result = await inventory.UpdateAsync(organizationId, propertyId, buildingId, actorId, id, request.ToInput(), ct);
        return result switch
        {
            UnitWriteResult.Saved => NoContent(),
            UnitWriteResult.DuplicateNumber => DuplicateNumber(),
            _ => NotFound()
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid propertyId, Guid buildingId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await inventory.ArchiveAsync(organizationId, propertyId, buildingId, actorId, id, ct) ? NoContent() : NotFound();
    }

    private bool TryGetActor(out Guid actorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    private ObjectResult DuplicateNumber() =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "This unit number is already in use in the building.");

    public sealed record UnitRequest(
        [Required, StringLength(50)] string Number,
        decimal? Area,
        [EnumDataType(typeof(UnitStatus))] UnitStatus Status = UnitStatus.Vacant) : IValidatableObject
    {
        public UnitInput ToInput() => new(Number.Trim(), Area, Status);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Area is decimal area && (area <= 0 || area > 9999999999.99m || decimal.Round(area, 2) != area))
            {
                yield return new ValidationResult(
                    "Area must be positive, no greater than 9999999999.99, and have at most two decimal places.",
                    [nameof(Area)]);
            }
        }
    }
}
