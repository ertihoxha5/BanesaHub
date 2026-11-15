using System.ComponentModel.DataAnnotations;

namespace BanesaHub.Web.Inventory;

public sealed record PropertyInput(
    [Required(ErrorMessage = "Shkruani emrin e pronës.")]
    [StringLength(200, ErrorMessage = "Emri mund të ketë deri në 200 shenja.")] string Name,
    [StringLength(300, ErrorMessage = "Adresa mund të ketë deri në 300 shenja.")] string? Address,
    [StringLength(100, ErrorMessage = "Kategoria mund të ketë deri në 100 shenja.")] string? Category);

public sealed record PropertyForm(Guid OrganizationId, Guid? PropertyId, PropertyInput Input, bool Archive = false);
