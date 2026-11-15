using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BanesaHub.Web.Inventory;

public enum UnitStatus
{
    [Display(Name = "E lirë")] Vacant = 1,
    [Display(Name = "E zënë")] Occupied = 2,
    [Display(Name = "E padisponueshme")] Unavailable = 3
}

public sealed record UnitRow(Guid Id, string Number, decimal? Area, UnitStatus Status);

public sealed record UnitList(Guid OrganizationId, Guid PropertyId, Guid BuildingId, string BuildingName,
    UnitRow[] Rows, int Page, bool HasNext);

public sealed record UnitInput(
    [Required(ErrorMessage = "Shkruani numrin e njësisë.")]
    [StringLength(50, ErrorMessage = "Numri mund të ketë deri në 50 shenja.")] string Number,
    [StringLength(13, ErrorMessage = "Sipërfaqja është shumë e gjatë.")] string? Area,
    [EnumDataType(typeof(UnitStatus), ErrorMessage = "Zgjidhni një gjendje të vlefshme.")]
    UnitStatus Status = UnitStatus.Vacant) : IValidatableObject
{
    public decimal? ParseArea() => decimal.TryParse(Area?.Trim().Replace(',', '.'),
        NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Area) &&
            (ParseArea() is not decimal value || value <= 0 || value > 9999999999.99m || decimal.Round(value, 2) != value))
        {
            yield return new ValidationResult(
                "Shkruani një sipërfaqe pozitive me deri në dy shifra pas presjes.", [nameof(Area)]);
        }
    }
}

public sealed record UnitForm(Guid OrganizationId, Guid PropertyId, Guid BuildingId, Guid? UnitId,
    UnitInput Input, bool Archive = false);
