using System.ComponentModel.DataAnnotations;
using System.Net;
using BanesaHub.Web.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.Web.Controllers;

[Route("organizations/{organizationId:guid}/tenants")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
public sealed class TenantsController(InventoryApi api) : Controller
{
    private const int PageSize = 20;

    [HttpGet("")]
    public Task<IActionResult> Index([FromRoute] Guid organizationId, CancellationToken ct,
        [StringLength(200)] string? search = null, [Range(1, 1 + int.MaxValue / PageSize)] int page = 1) =>
        WithSession(organizationId, async session =>
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return View("~/Views/Inventory/Index.cshtml",
                    new PropertyList(organizationId, Message: "Kërkimi ose numri i faqes nuk është i vlefshëm."));
            }

            var (status, rows) = await api.ListTenantsAsync(organizationId, session,
                (page - 1) * PageSize, PageSize + 1, search?.Trim(), ct);
            return status == HttpStatusCode.OK
                ? View(new TenantList(organizationId, rows.Take(PageSize).ToArray(), search, page,
                    rows.Length > PageSize && page < 1 + int.MaxValue / PageSize))
                : Failure(organizationId, status);
        }, ct);

    [HttpGet("{tenantId:guid}")]
    public Task<IActionResult> Profile([FromRoute] Guid organizationId, [FromRoute] Guid tenantId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, tenant) = await api.FindTenantAsync(organizationId, tenantId, session, ct);
            return status == HttpStatusCode.OK && tenant is not null
                ? View(new TenantProfile(organizationId, tenant))
                : Failure(organizationId, status);
        }, ct);

    [HttpGet("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, _) = await api.ListTenantsAsync(organizationId, session, 0, 1, null, ct);
            return status == HttpStatusCode.OK
                ? Show(new TenantForm(organizationId, null, new TenantInput("", null, null)))
                : Failure(organizationId, status);
        }, ct);

    [HttpPost("new")]
    public Task<IActionResult> Create([FromRoute] Guid organizationId, [FromForm] TenantInput input, CancellationToken ct) =>
        Save(new TenantForm(organizationId, null, input), HttpMethod.Post, ct);

    [HttpGet("{tenantId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid tenantId, CancellationToken ct) =>
        Load(organizationId, tenantId, archive: false, ct);

    [HttpPost("{tenantId:guid}/edit")]
    public Task<IActionResult> Edit([FromRoute] Guid organizationId, [FromRoute] Guid tenantId, [FromForm] TenantInput input, CancellationToken ct) =>
        Save(new TenantForm(organizationId, tenantId, input), HttpMethod.Put, ct);

    [HttpGet("{tenantId:guid}/archive")]
    public Task<IActionResult> Archive([FromRoute] Guid organizationId, [FromRoute] Guid tenantId, CancellationToken ct) =>
        Load(organizationId, tenantId, archive: true, ct);

    [HttpPost("{tenantId:guid}/archive")]
    [ActionName(nameof(Archive))]
    public Task<IActionResult> ConfirmArchive([FromRoute] Guid organizationId, [FromRoute] Guid tenantId, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var status = await api.WriteTenantAsync(organizationId, tenantId, session, HttpMethod.Delete, null, ct);
            return status == HttpStatusCode.NoContent
                ? BackToList(organizationId)
                : Failure(organizationId, status);
        }, ct);

    private Task<IActionResult> Load(Guid organizationId, Guid tenantId, bool archive, CancellationToken ct) =>
        WithSession(organizationId, async session =>
        {
            var (status, tenant) = await api.FindTenantAsync(organizationId, tenantId, session, ct);
            if (status != HttpStatusCode.OK || tenant is null)
            {
                return Failure(organizationId, status);
            }

            return Show(new TenantForm(organizationId, tenantId,
                new TenantInput(tenant.FullName, tenant.Email, tenant.Phone), archive));
        }, ct);

    private Task<IActionResult> Save(TenantForm form, HttpMethod method, CancellationToken ct) =>
        WithSession(form.OrganizationId, async session =>
        {
            if (!ModelState.IsValid)
            {
                return Show(form);
            }

            var status = await api.WriteTenantAsync(form.OrganizationId, form.TenantId, session, method, form.Input, ct);
            if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
            {
                return BackToList(form.OrganizationId);
            }

            if (status == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError("", "Kontakti nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");
                return Show(form);
            }

            return Failure(form.OrganizationId, status);
        }, ct);

    private async Task<IActionResult> WithSession(Guid organizationId, Func<string, Task<IActionResult>> action, CancellationToken ct)
    {
        if (Request.Cookies[InventoryApi.SessionCookie] is not { Length: > 0 } session)
        {
            return RedirectToAction(nameof(InventoryController.Index), "Inventory", new { organizationId });
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

    private IActionResult Show(TenantForm form) => View("Form", form);

    private IActionResult BackToList(Guid organizationId) =>
        RedirectToAction(nameof(Index), new { organizationId });

    private IActionResult Failure(Guid organizationId, HttpStatusCode status)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect)
        {
            return RedirectToAction(nameof(InventoryController.Index), "Inventory", new { organizationId });
        }

        var missing = status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden;
        Response.StatusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        return View("~/Views/Inventory/Index.cshtml", new PropertyList(organizationId, Message: missing
            ? "Kontakti ose organizata nuk u gjet, ose nuk keni qasje."
            : "Shërbimi nuk është i disponueshëm. Provoni përsëri."));
    }
}
