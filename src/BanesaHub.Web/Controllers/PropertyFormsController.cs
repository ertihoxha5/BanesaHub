using System.Net;
using BanesaHub.Web.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.Web.Controllers;

[Route("organizations/{organizationId:guid}/properties")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
public sealed class PropertyFormsController(InventoryApi api) : Controller
{
    [HttpGet("new")]
    public Task<IActionResult> Create(Guid organizationId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, _) = await api.ListAsync(organizationId, session, 0, 1, null, null, ct);
            return status == HttpStatusCode.OK
                ? Show(new PropertyForm(organizationId, null, new PropertyInput("", null, null)))
                : Failure(organizationId, status);
        }, ct);

    [HttpPost("new")]
    public Task<IActionResult> Create(Guid organizationId, [FromForm] PropertyInput input, CancellationToken ct) =>
        Save(new PropertyForm(organizationId, null, input), HttpMethod.Post, ct);

    [HttpGet("{propertyId:guid}/edit")]
    public Task<IActionResult> Edit(Guid organizationId, Guid propertyId, CancellationToken ct) =>
        Load(organizationId, propertyId, archive: false, ct);

    [HttpPost("{propertyId:guid}/edit")]
    public Task<IActionResult> Edit(Guid organizationId, Guid propertyId, [FromForm] PropertyInput input, CancellationToken ct) =>
        Save(new PropertyForm(organizationId, propertyId, input), HttpMethod.Put, ct);

    [HttpGet("{propertyId:guid}/archive")]
    public Task<IActionResult> Archive(Guid organizationId, Guid propertyId, CancellationToken ct) =>
        Load(organizationId, propertyId, archive: true, ct);

    [HttpPost("{propertyId:guid}/archive")]
    [ActionName(nameof(Archive))]
    public Task<IActionResult> ConfirmArchive(Guid organizationId, Guid propertyId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var status = await api.WritePropertyAsync(organizationId, propertyId, session, HttpMethod.Delete, null, ct);
            return status == HttpStatusCode.NoContent
                ? BackToList(organizationId)
                : Failure(organizationId, status);
        }, ct);

    private Task<IActionResult> Load(Guid organizationId, Guid propertyId, bool archive, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, property) = await api.FindPropertyAsync(organizationId, propertyId, session, ct);
            if (status != HttpStatusCode.OK || property is null)
            {
                return Failure(organizationId, status);
            }

            return Show(new PropertyForm(organizationId, propertyId,
                new PropertyInput(property.Name, property.Address, property.Category), archive));
        }, ct);

    private Task<IActionResult> Save(PropertyForm form, HttpMethod method, CancellationToken ct) =>
        WithSession(form.OrganizationId, async session =>
        {
            if (!ModelState.IsValid)
            {
                return Show(form);
            }

            var status = await api.WritePropertyAsync(form.OrganizationId, form.PropertyId, session, method, form.Input, ct);
            if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
            {
                return BackToList(form.OrganizationId);
            }

            if (status == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError("", "Prona nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");
                return Show(form);
            }

            return Failure(form.OrganizationId, status);
        }, ct);

    private async Task<IActionResult> WithSession(Guid organizationId, Func<string, Task<IActionResult>> action, CancellationToken ct)
    {
        if (Request.Cookies[InventoryApi.SessionCookie] is not { Length: > 0 } session)
        {
            return BackToList(organizationId);
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

    private IActionResult Show(PropertyForm form) => View("Form", form);

    private IActionResult BackToList(Guid organizationId) =>
        RedirectToAction(nameof(InventoryController.Index), "Inventory", new { organizationId });

    private IActionResult Failure(Guid organizationId, HttpStatusCode status)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect)
        {
            return BackToList(organizationId);
        }

        var missing = status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden;
        Response.StatusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        return View("~/Views/Inventory/Index.cshtml", new PropertyList(organizationId, Message: missing
            ? "Prona ose organizata nuk u gjet, ose nuk keni qasje."
            : "Shërbimi nuk është i disponueshëm. Provoni përsëri."));
    }
}
