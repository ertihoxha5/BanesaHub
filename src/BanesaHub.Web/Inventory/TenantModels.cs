using System.ComponentModel.DataAnnotations;

namespace BanesaHub.Web.Inventory;

public sealed record TenantRow(Guid Id, string FullName, string? Email, string? Phone);
public sealed record TenantList(Guid OrganizationId, TenantRow[] Rows, string? Search, int Page, bool HasNext);
public sealed record TenantProfile(Guid OrganizationId, TenantRow Contact);

public sealed record TenantInput(
    [Required(ErrorMessage = "Shkruani emrin dhe mbiemrin.")]
    [StringLength(200, ErrorMessage = "Emri mund të ketë deri në 200 shenja.")] string FullName,
    [StringLength(256, ErrorMessage = "Emaili mund të ketë deri në 256 shenja.")]
    [EmailAddress(ErrorMessage = "Adresa e emailit nuk është e vlefshme.")] string? Email,
    [StringLength(32, ErrorMessage = "Telefoni mund të ketë deri në 32 shenja.")] string? Phone);

public sealed record TenantForm(Guid OrganizationId, Guid? TenantId, TenantInput Input, bool Archive = false);
