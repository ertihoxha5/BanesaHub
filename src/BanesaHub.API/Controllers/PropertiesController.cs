using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations/{organizationId:guid}/properties")]
public sealed class PropertiesController(IPropertyInventory inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, CancellationToken ct,
        [Range(0, int.MaxValue)] int offset = 0, [Range(1, 100)] int limit = 25)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var properties = await inventory.ListAsync(organizationId, actorId, offset, limit, ct);
        return properties is null ? NotFound() : Ok(properties);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var property = await inventory.FindAsync(organizationId, actorId, id, ct);
        return property is null ? NotFound() : Ok(property);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, PropertyRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var property = await inventory.CreateAsync(organizationId, actorId, request.ToInput(), ct);
        return property is null ? NotFound() : CreatedAtAction(nameof(Get), new { organizationId, property.Id }, property);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, PropertyRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await inventory.UpdateAsync(organizationId, actorId, id, request.ToInput(), ct) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await inventory.ArchiveAsync(organizationId, actorId, id, ct) ? NoContent() : NotFound();
    }

    private bool TryGetActor(out Guid actorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    public sealed record PropertyRequest(
        [Required, StringLength(200)] string Name,
        [StringLength(300)] string? Address,
        [StringLength(100)] string? Category)
    {
        public PropertyInput ToInput() => new(Name.Trim(), Normalize(Address), Normalize(Category));

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
