using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations/{organizationId:guid}/properties/{propertyId:guid}/buildings")]
public sealed class BuildingsController(IBuildingInventory inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, Guid propertyId, CancellationToken ct,
        [Range(0, int.MaxValue)] int offset = 0, [Range(1, 100)] int limit = 25)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var buildings = await inventory.ListAsync(organizationId, propertyId, actorId, offset, limit, ct);
        return buildings is null ? NotFound() : Ok(buildings);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid propertyId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var building = await inventory.FindAsync(organizationId, propertyId, actorId, id, ct);
        return building is null ? NotFound() : Ok(building);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, Guid propertyId, BuildingRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var building = await inventory.CreateAsync(organizationId, propertyId, actorId, request.ToInput(), ct);
        return building is null ? NotFound() : CreatedAtAction(nameof(Get), new { organizationId, propertyId, building.Id }, building);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid propertyId, Guid id, BuildingRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await inventory.UpdateAsync(organizationId, propertyId, actorId, id, request.ToInput(), ct) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid propertyId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await inventory.ArchiveAsync(organizationId, propertyId, actorId, id, ct) ? NoContent() : NotFound();
    }

    private bool TryGetActor(out Guid actorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    public sealed record BuildingRequest(
        [Required, StringLength(200)] string Name,
        [Range(1, int.MaxValue)] int? FloorCount)
    {
        public BuildingInput ToInput() => new(Name.Trim(), FloorCount);
    }
}
