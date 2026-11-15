using System.Globalization;
using System.Net;
using BanesaHub.Web.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.Web.Controllers;

[Route("organizations/{organizationId:guid}/properties/{propertyId:guid}/buildings/{buildingId:guid}/units")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
public sealed class UnitsController(InventoryApi api) : Controller
{
    private const int PageSize = 20;
    private static readonly CultureInfo Albanian = CultureInfo.GetCultureInfo("sq-XK");

    [HttpGet("")]
    public Task<IActionResult> Index([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, CancellationToken ct, int page = 1) =>
        WithSession(organizationId, async session =>
        {
            if (!ModelState.IsValid || page < 1 || page > 1 + int.MaxValue / PageSize)
            {
                return Error(organizationId, 400, "Numri i faqes nuk është i vlefshëm.");
            }

            var (status, building) = await api.FindBuildingAsync(organizationId, propertyId, buildingId, session, ct);
            if (status != HttpStatusCode.OK || building is null)
            {
                return Failure(organizationId, status);
            }

            var (listStatus, rows) = await api.ListUnitsAsync(organizationId, propertyId, buildingId, session,
                (page - 1) * PageSize, PageSize + 1, ct);
            if (listStatus != HttpStatusCode.OK)
            {
                return Failure(organizationId, listStatus);
            }

            return View(new UnitList(organizationId, propertyId, buildingId, building.Name, rows.Take(PageSize).ToArray(),
                page, rows.Length > PageSize && page < 1 + int.MaxValue / PageSize));
        }, ct);

    [HttpGet("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, _) = await api.FindBuildingAsync(organizationId, propertyId, buildingId, session, ct);
            return status == HttpStatusCode.OK
                ? Show(new UnitForm(organizationId, propertyId, buildingId, null, new UnitInput("", null)))
                : Failure(organizationId, status);
        }, ct);

    [HttpPost("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromForm] UnitInput input, CancellationToken ct) =>
        Save(new UnitForm(organizationId, propertyId, buildingId, null, input), HttpMethod.Post, ct);

    [HttpGet("{unitId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromRoute] Guid unitId, CancellationToken ct) =>
        Load(organizationId, propertyId, buildingId, unitId, false, ct);

    [HttpPost("{unitId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromRoute] Guid unitId, [FromForm] UnitInput input,
        CancellationToken ct) => Save(new UnitForm(organizationId, propertyId, buildingId, unitId, input), HttpMethod.Put, ct);

    [HttpGet("{unitId:guid}/archive")]
    public Task<IActionResult> Archive([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromRoute] Guid unitId, CancellationToken ct) =>
        Load(organizationId, propertyId, buildingId, unitId, true, ct);

    [HttpPost("{unitId:guid}/archive")]
    [ActionName(nameof(Archive))]
    public Task<IActionResult> ConfirmArchive([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromRoute] Guid unitId, CancellationToken ct) =>
        Save(new UnitForm(organizationId, propertyId, buildingId, unitId, new UnitInput("", null), Archive: true),
            HttpMethod.Delete, ct);

    private Task<IActionResult> Load(Guid organizationId, Guid propertyId, Guid buildingId, Guid unitId, bool archive, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, unit) = await api.FindUnitAsync(organizationId, propertyId, buildingId, unitId, session, ct);
            return status == HttpStatusCode.OK && unit is not null
                ? Show(new UnitForm(organizationId, propertyId, buildingId, unitId,
                    new UnitInput(unit.Number, unit.Area?.ToString("0.##", Albanian), unit.Status), archive))
                : Failure(organizationId, status);
        }, ct);

    private Task<IActionResult> Save(UnitForm form, HttpMethod method, CancellationToken ct) =>
        WithSession(form.OrganizationId, async session =>
        {
            if (!ModelState.IsValid && !form.Archive)
            {
                return Show(form);
            }

            var status = await api.WriteUnitAsync(form, session, method, ct);
            if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
            {
                return RedirectToAction(nameof(Index), new { form.OrganizationId, form.PropertyId, form.BuildingId });
            }

            if (!form.Archive && status is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                if (status == HttpStatusCode.Conflict)
                {
                    Response.StatusCode = StatusCodes.Status409Conflict;
                    ModelState.AddModelError("Input.Number", "Ky numër është tashmë në përdorim në këtë ndërtesë.");
                }
                else
                {
                    ModelState.AddModelError("", "Njësia nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");
                }
                return Show(form);
            }

            return Failure(form.OrganizationId, status);
        }, ct);

    private async Task<IActionResult> WithSession(Guid organizationId, Func<string, Task<IActionResult>> action, CancellationToken ct)
    {
        if (Request.Cookies[InventoryApi.SessionCookie] is not { Length: > 0 } session)
        {
            return BackToProperties(organizationId);
        }

        try
        {
            return await action(session);
        }
        catch (HttpRequestException) when (!ct.IsCancellationRequested)
        {
            return Failure(organizationId, HttpStatusCode.ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failure(organizationId, HttpStatusCode.ServiceUnavailable);
        }
    }

    private ViewResult Show(UnitForm form) => View("Form", form);

    private RedirectToActionResult BackToProperties(Guid organizationId) =>
        RedirectToAction(nameof(InventoryController.Index), "Inventory", new { organizationId });

    private IActionResult Failure(Guid organizationId, HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Redirect => BackToProperties(organizationId),
        HttpStatusCode.NotFound or HttpStatusCode.Forbidden =>
            Error(organizationId, 404, "Njësia, ndërtesa ose prona nuk u gjet, ose nuk keni qasje."),
        _ => Error(organizationId, 503, "Shërbimi nuk është i disponueshëm. Provoni përsëri.")
    };

    private ViewResult Error(Guid organizationId, int status, string message)
    {
        Response.StatusCode = status;
        return View("~/Views/Inventory/Index.cshtml", new PropertyList(organizationId, Message: message));
    }
}
