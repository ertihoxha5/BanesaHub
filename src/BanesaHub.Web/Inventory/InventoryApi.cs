using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace BanesaHub.Web.Inventory;

public sealed class InventoryApi(HttpClient http)
{
    public const string SessionCookie = ".AspNetCore.Identity.Application";

    public async Task<(HttpStatusCode Status, PropertyRow[] Rows)> ListAsync(
        Guid organizationId, string? session, int offset, int limit, string? search,
        PropertyFilter? status, CancellationToken ct)
    {
        var path = QueryHelpers.AddQueryString($"/api/v1/organizations/{organizationId}/properties",
            new Dictionary<string, string?>
            {
                ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
                ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
                ["search"] = search,
                ["status"] = status?.ToString()
            });
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (session is not null)
        {
            request.Headers.Add("Cookie", $"{SessionCookie}={session}");
        }

        using var response = await http.SendAsync(request, ct);
        var rows = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PropertyRow[]>(ct) ?? []
            : [];
        return (response.StatusCode, rows);
    }

    public async Task<(HttpStatusCode Status, PropertyRow? Property)> FindPropertyAsync(
        Guid organizationId, Guid propertyId, string session, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/properties/{propertyId}");
        request.Headers.Add("Cookie", $"{SessionCookie}={session}");
        using var response = await http.SendAsync(request, ct);
        var property = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<PropertyRow>(ct)
            : null;
        return (response.StatusCode, property);
    }

    public Task<HttpStatusCode> WritePropertyAsync(
        Guid organizationId, Guid? propertyId, string session, HttpMethod method, PropertyInput? input, CancellationToken ct)
    {
        var path = $"/api/v1/organizations/{organizationId}/properties";
        return WriteAsync(propertyId.HasValue ? $"{path}/{propertyId}" : path, session, method, input, ct);
    }

    public Task<HttpStatusCode> WriteBuildingAsync(Guid organizationId, Guid propertyId, Guid? buildingId,
        string session, HttpMethod method, BuildingInput? input, CancellationToken ct)
    {
        var path = BuildingsPath(organizationId, propertyId);
        return WriteAsync(buildingId.HasValue ? $"{path}/{buildingId}" : path, session, method, input, ct);
    }

    public async Task<(HttpStatusCode Status, BuildingRow[] Rows)> ListBuildingsAsync(
        Guid organizationId, Guid propertyId, string session, int offset, int limit, CancellationToken ct)
    {
        var path = FormattableString.Invariant($"{BuildingsPath(organizationId, propertyId)}?offset={offset}&limit={limit}");
        var (status, rows) = await ReadAsync<BuildingRow[]>(path, session, ct);
        return (status, rows ?? []);
    }

    public Task<(HttpStatusCode Status, BuildingRow? Row)> FindBuildingAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, string session, CancellationToken ct) =>
        ReadAsync<BuildingRow>($"{BuildingsPath(organizationId, propertyId)}/{buildingId}", session, ct);

    public async Task<(HttpStatusCode Status, UnitRow[] Rows)> ListUnitsAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, string session, int offset, int limit, CancellationToken ct)
    {
        var path = FormattableString.Invariant($"{UnitsPath(organizationId, propertyId, buildingId)}?offset={offset}&limit={limit}");
        var (status, rows) = await ReadAsync<UnitRow[]>(path, session, ct);
        return (status, rows ?? []);
    }

    public Task<(HttpStatusCode Status, UnitRow? Row)> FindUnitAsync(
        Guid organizationId, Guid propertyId, Guid buildingId, Guid unitId, string session, CancellationToken ct) =>
        ReadAsync<UnitRow>($"{UnitsPath(organizationId, propertyId, buildingId)}/{unitId}", session, ct);

    public Task<HttpStatusCode> WriteUnitAsync(UnitForm form, string session, HttpMethod method, CancellationToken ct)
    {
        var path = UnitsPath(form.OrganizationId, form.PropertyId, form.BuildingId);
        return WriteAsync(form.UnitId is Guid id ? $"{path}/{id}" : path, session, method,
            form.Archive ? null : new { form.Input.Number, Area = form.Input.ParseArea(), form.Input.Status }, ct);
    }

    private static string UnitsPath(Guid organizationId, Guid propertyId, Guid buildingId) =>
        $"{BuildingsPath(organizationId, propertyId)}/{buildingId}/units";

    private static string BuildingsPath(Guid organizationId, Guid propertyId) =>
        $"/api/v1/organizations/{organizationId}/properties/{propertyId}/buildings";

    public async Task<(HttpStatusCode Status, TenantRow[] Rows)> ListTenantsAsync(Guid organizationId, string session,
        int offset, int limit, string? search, CancellationToken ct)
    {
        var path = QueryHelpers.AddQueryString(TenantsPath(organizationId), new Dictionary<string, string?>
        {
            ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
            ["search"] = search
        });
        var (status, rows) = await ReadAsync<TenantRow[]>(path, session, ct);
        return (status, rows ?? []);
    }

    public Task<(HttpStatusCode Status, TenantRow? Row)> FindTenantAsync(
        Guid organizationId, Guid tenantId, string session, CancellationToken ct) =>
        ReadAsync<TenantRow>($"{TenantsPath(organizationId)}/{tenantId}", session, ct);

    public Task<HttpStatusCode> WriteTenantAsync(Guid organizationId, Guid? tenantId,
        string session, HttpMethod method, TenantInput? input, CancellationToken ct)
    {
        var path = TenantsPath(organizationId);
        return WriteAsync(tenantId is Guid id ? $"{path}/{id}" : path, session, method, input, ct);
    }

    private static string TenantsPath(Guid organizationId) => $"/api/v1/organizations/{organizationId}/tenants";

    private async Task<(HttpStatusCode Status, T? Data)> ReadAsync<T>(
        string path, string session, CancellationToken ct) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{SessionCookie}={session}");
        using var response = await http.SendAsync(request, ct);
        var data = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<T>(ct)
            : null;
        return (response.StatusCode, data);
    }

    private async Task<HttpStatusCode> WriteAsync(
        string path, string session, HttpMethod method, object? input, CancellationToken ct)
    {
        var tokenUri = new Uri(http.BaseAddress!, "/api/v1/auth/antiforgery");
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, tokenUri);
        tokenRequest.Headers.Add("Cookie", $"{SessionCookie}={session}");
        using var tokenResponse = await http.SendAsync(tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return tokenResponse.StatusCode;
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryToken>(ct);
        if (string.IsNullOrEmpty(token?.RequestToken))
        {
            return HttpStatusCode.BadGateway;
        }

        var cookies = new CookieContainer();
        cookies.Add(tokenUri, new Cookie(SessionCookie, session, "/") { Secure = true });
        CaptureCookies(tokenResponse, cookies);
        var uri = new Uri(http.BaseAddress!, path);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Cookie", cookies.GetCookieHeader(uri));
        request.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        if (input is not null)
        {
            request.Content = JsonContent.Create(input);
        }

        using var response = await http.SendAsync(request, ct);
        return response.StatusCode;
    }

    public async Task<(HttpStatusCode Status, string? Session)> SignInAsync(
        string email, string password, CancellationToken ct)
    {
        var cookies = new CookieContainer();
        using var tokenResponse = await http.GetAsync("/api/v1/auth/antiforgery", ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return (tokenResponse.StatusCode, null);
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryToken>(ct);
        if (token is null)
        {
            return (HttpStatusCode.BadGateway, null);
        }

        CaptureCookies(tokenResponse, cookies);
        var loginUri = new Uri(http.BaseAddress!, "/api/v1/auth/login");
        using var login = new HttpRequestMessage(HttpMethod.Post, loginUri)
        {
            Content = JsonContent.Create(new { email, password })
        };
        login.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        login.Headers.Add("Cookie", cookies.GetCookieHeader(loginUri));
        using var response = await http.SendAsync(login, ct);
        CaptureCookies(response, cookies);
        return (response.StatusCode, cookies.GetCookies(loginUri)[SessionCookie]?.Value);
    }

    private static void CaptureCookies(HttpResponseMessage response, CookieContainer cookies)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            foreach (var value in values)
            {
                cookies.SetCookies(response.RequestMessage!.RequestUri!, value);
            }
        }
    }

    private sealed record AntiforgeryToken(string RequestToken);
}
