using System.ComponentModel.DataAnnotations;
using System.Net;
using BanesaHub.Web.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BanesaHub.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InventoryController(InventoryApi api) : Controller
{
    private const int PageSize = 20;
    private const string Unavailable = "Shërbimi nuk është i disponueshëm. Provoni përsëri.";

    [HttpGet("/")]
    public IActionResult Home(Guid? organizationId)
    {
        if (ModelState.IsValid && organizationId is Guid id && id != Guid.Empty)
        {
            return RedirectToAction(nameof(Index), new { organizationId = id });
        }

        return View("Index", new PropertyList(
            Message: Request.Query.ContainsKey("organizationId") ? "Shkruani një identifikues të vlefshëm të organizatës." : null));
    }

    [HttpGet("/organizations/{organizationId:guid}/properties")]
    public async Task<IActionResult> Index(Guid organizationId, CancellationToken ct,
        [StringLength(200)] string? search = null, PropertyFilter? status = null,
        [Range(1, 1 + int.MaxValue / PageSize)] int page = 1)
    {
        if (!ModelState.IsValid || (status.HasValue && !Enum.IsDefined(status.Value)))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(new PropertyList(organizationId, Message: "Filtrat ose numri i faqes nuk janë të vlefshëm."));
        }

        var session = Request.Cookies[InventoryApi.SessionCookie];
        if (string.IsNullOrEmpty(session))
        {
            return View(new PropertyList(organizationId, SignInRequired: true));
        }

        try
        {
            var (code, rows) = await api.ListAsync(organizationId, session, (page - 1) * PageSize, PageSize + 1,
                search?.Trim(), status, ct);
            if (code is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect)
            {
                return View(new PropertyList(organizationId, SignInRequired: true));
            }

            if (code is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View(new PropertyList(organizationId, Message: "Organizata nuk u gjet ose nuk keni qasje te pronat e saj."));
            }

            if (code != HttpStatusCode.OK)
            {
                return ServiceUnavailable(organizationId);
            }

            return View(new PropertyList(organizationId, search, status, page,
                rows.Length > PageSize && page < 1 + int.MaxValue / PageSize, rows.Take(PageSize).ToArray()));
        }
        catch (HttpRequestException) when (!ct.IsCancellationRequested)
        {
            return ServiceUnavailable(organizationId);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ServiceUnavailable(organizationId);
        }
    }

    [HttpPost("/organizations/{organizationId:guid}/properties/sign-in")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(Guid organizationId, [FromForm] Credentials credentials, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return SignInError(organizationId, "Kontrolloni adresën e emailit dhe fjalëkalimin.");
        }

        try
        {
            var (code, session) = await api.SignInAsync(credentials.Email, credentials.Password, ct);
            if (code == HttpStatusCode.NoContent && session is not null)
            {
                Response.Cookies.Append(InventoryApi.SessionCookie, session, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });
                return RedirectToAction(nameof(Index), new { organizationId });
            }

            var message = code switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest =>
                    "Hyrja dështoi. Kontrolloni adresën e emailit dhe fjalëkalimin.",
                HttpStatusCode.TooManyRequests => "Keni bërë shumë përpjekje. Provoni më vonë.",
                _ => Unavailable
            };
            return SignInError(organizationId, message);
        }
        catch (HttpRequestException) when (!ct.IsCancellationRequested)
        {
            return SignInError(organizationId, Unavailable);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return SignInError(organizationId, Unavailable);
        }
    }

    [HttpPost("/sign-out")]
    [ValidateAntiForgeryToken]
    public IActionResult SignOutInventory()
    {
        Response.Cookies.Delete(InventoryApi.SessionCookie, new CookieOptions { Secure = true, Path = "/" });
        return RedirectToAction(nameof(Home));
    }

    private IActionResult SignInError(Guid id, string message) =>
        View("Index", new PropertyList(id, SignInRequired: true, Message: message));

    private IActionResult ServiceUnavailable(Guid id)
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("Index", new PropertyList(id, Message: Unavailable));
    }

    public sealed record Credentials(
        [Required, EmailAddress, StringLength(256)] string Email,
        [Required, StringLength(128)] string Password);
}
