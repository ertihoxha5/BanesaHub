using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BanesaHub.Application.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.API.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
[Route("api/v1/organizations/{organizationId:guid}/tenants")]
public sealed class TenantsController(ITenantContacts contacts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, CancellationToken ct,
        [Range(0, int.MaxValue)] int offset = 0, [Range(1, 100)] int limit = 25,
        [StringLength(200)] string? search = null)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var tenants = await contacts.ListAsync(organizationId, actorId, offset, limit, search, ct);
        return tenants is null ? NotFound() : Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var tenant = await contacts.FindAsync(organizationId, actorId, id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, TenantRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        var tenant = await contacts.CreateAsync(organizationId, actorId, request.ToInput(), ct);
        return tenant is null ? NotFound() : CreatedAtAction(nameof(Get), new { organizationId, tenant.Id }, tenant);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, TenantRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await contacts.UpdateAsync(organizationId, actorId, id, request.ToInput(), ct) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid id, CancellationToken ct)
    {
        if (!TryGetActor(out var actorId))
        {
            return Unauthorized();
        }

        return await contacts.ArchiveAsync(organizationId, actorId, id, ct) ? NoContent() : NotFound();
    }

    private bool TryGetActor(out Guid actorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    public sealed record TenantRequest(
        [Required, StringLength(200)] string FullName,
        [StringLength(256)] string? Email,
        [StringLength(32)] string? Phone) : IValidatableObject
    {
        public TenantInput ToInput() => new(FullName.Trim(), Normalize(Email), Normalize(Phone));

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Normalize(Email) is string email && !new EmailAddressAttribute().IsValid(email))
            {
                yield return new ValidationResult("Adresa e emailit nuk është e vlefshme.", [nameof(Email)]);
            }
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
