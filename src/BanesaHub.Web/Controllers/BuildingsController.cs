using System.Net;
using BanesaHub.Web.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.Web.Controllers;

[Route("organizations/{organizationId:guid}/properties/{propertyId:guid}/buildings")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
public sealed class BuildingsController(InventoryApi api) : Controller
{
    private const int PageSize = 20;

    [HttpGet("")]
    public Task<IActionResult> Index([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, CancellationToken ct, int page = 1) =>
        WithSession(organizationId, async session =>
        {
            if (!ModelState.IsValid || page < 1 || page > 1 + int.MaxValue / PageSize)
            {
                return Error(organizationId, 400, "Numri i faqes nuk është i vlefshëm.");
            }

            var (status, property) = await api.FindPropertyAsync(organizationId, propertyId, session, ct);
            if (status != HttpStatusCode.OK || property is null)
            {
                return Failure(organizationId, status);
            }

            var (listStatus, rows) = await api.ListBuildingsAsync(organizationId, propertyId, session,
                (page - 1) * PageSize, PageSize + 1, ct);
            if (listStatus != HttpStatusCode.OK)
            {
                return Failure(organizationId, listStatus);
            }

            return View(new BuildingList(organizationId, propertyId, property.Name, rows.Take(PageSize).ToArray(),
                page, rows.Length > PageSize && page < 1 + int.MaxValue / PageSize));
        }, ct);

    [HttpGet("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, _) = await api.FindPropertyAsync(organizationId, propertyId, session, ct);
            return status == HttpStatusCode.OK
                ? Show(new BuildingForm(organizationId, propertyId, null, new BuildingInput("", null)))
                : Failure(organizationId, status);
        }, ct);

    [HttpPost("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromForm] BuildingInput input, CancellationToken ct) =>
        Save(new BuildingForm(organizationId, propertyId, null, input), HttpMethod.Post, ct);

    [HttpGet("{buildingId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, CancellationToken ct) =>
        Load(organizationId, propertyId, buildingId, false, ct);

    [HttpPost("{buildingId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, [FromForm] BuildingInput input,
        CancellationToken ct) => Save(new BuildingForm(organizationId, propertyId, buildingId, input), HttpMethod.Put, ct);

    [HttpGet("{buildingId:guid}/archive")]
    public Task<IActionResult> Archive([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, CancellationToken ct) =>
        Load(organizationId, propertyId, buildingId, true, ct);

    [HttpPost("{buildingId:guid}/archive")]
    [ActionName(nameof(Archive))]
    public Task<IActionResult> ConfirmArchive([FromRoute] Guid organizationId, [FromRoute] Guid propertyId, [FromRoute] Guid buildingId, CancellationToken ct) =>
        Save(new BuildingForm(organizationId, propertyId, buildingId, new BuildingInput("", null), Archive: true),
            HttpMethod.Delete, ct);

    private Task<IActionResult> Load(Guid organizationId, Guid propertyId, Guid buildingId, bool archive, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, building) = await api.FindBuildingAsync(organizationId, propertyId, buildingId, session, ct);
            return status == HttpStatusCode.OK && building is not null
                ? Show(new BuildingForm(organizationId, propertyId, buildingId,
                    new BuildingInput(building.Name, building.FloorCount), archive))
                : Failure(organizationId, status);
        }, ct);

    private Task<IActionResult> Save(BuildingForm form, HttpMethod method, CancellationToken ct) =>
        WithSession(form.OrganizationId, async session =>
        {
            if (!ModelState.IsValid && !form.Archive)
            {
                return Show(form);
            }

            var status = await api.WriteBuildingAsync(form.OrganizationId, form.PropertyId, form.BuildingId,
                session, method, form.Archive ? null : form.Input, ct);
            if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
            {
                return RedirectToAction(nameof(Index), new { form.OrganizationId, form.PropertyId });
            }

            if (status == HttpStatusCode.BadRequest && !form.Archive)
            {
                ModelState.AddModelError("", "Ndërtesa nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");
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

    private ViewResult Show(BuildingForm form) => View("Form", form);

    private RedirectToActionResult BackToProperties(Guid organizationId) =>
        RedirectToAction(nameof(InventoryController.Index), "Inventory", new { organizationId });

    private IActionResult Failure(Guid organizationId, HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Redirect => BackToProperties(organizationId),
        HttpStatusCode.NotFound or HttpStatusCode.Forbidden =>
            Error(organizationId, 404, "Ndërtesa ose prona nuk u gjet, ose nuk keni qasje."),
        _ => Error(organizationId, 503, "Shërbimi nuk është i disponueshëm. Provoni përsëri.")
    };

    private ViewResult Error(Guid organizationId, int status, string message)
    {
        Response.StatusCode = status;
        return View("~/Views/Inventory/Index.cshtml", new PropertyList(organizationId, Message: message));
    }
}
