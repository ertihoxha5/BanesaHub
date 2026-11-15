using System.ComponentModel.DataAnnotations;

namespace BanesaHub.Web.Inventory;

public sealed record BuildingRow(Guid Id, string Name, int? FloorCount);
public sealed record BuildingList(Guid OrganizationId, Guid PropertyId, string PropertyName,
    BuildingRow[] Rows, int Page, bool HasNext);

public sealed record BuildingInput(
    [Required(ErrorMessage = "Shkruani emrin e ndërtesës.")]
    [StringLength(200, ErrorMessage = "Emri mund të ketë deri në 200 shenja.")] string Name,
    [Range(1, int.MaxValue, ErrorMessage = "Numri i kateve duhet të jetë pozitiv.")] int? FloorCount);

public sealed record BuildingForm(Guid OrganizationId, Guid PropertyId, Guid? BuildingId,
    BuildingInput Input, bool Archive = false);
