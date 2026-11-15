using Microsoft.Data.SqlClient;
using BanesaHub.API.Auth;
using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Http.Json;
using BanesaHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BanesaHub.IntegrationTests;

public sealed class AuthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountFlowEnforcesAntiforgeryAndLockout(bool lockAccount)
    {
        using var host = CreateHost();
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var credentials = new { email = "test@example.test", password = $"Test!1{Guid.NewGuid():N}" };
        using var noToken = await client.PostAsJsonAsync("/api/v1/auth/register", credentials);
        Assert.True(noToken.StatusCode == HttpStatusCode.BadRequest, await noToken.Content.ReadAsStringAsync());

        await RefreshToken(client);
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register", credentials);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        if (lockAccount)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                using var invalid = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new { credentials.email, password = "invalid" });
                Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
            }
        }

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", credentials);
        Assert.Equal(lockAccount ? HttpStatusCode.Unauthorized : HttpStatusCode.NoContent, login.StatusCode);

        using var auditScope = host.Services.CreateScope();
        var auditDatabase = auditScope.ServiceProvider.GetRequiredService<BanesaHubDbContext>();
        var account = await auditDatabase.Users.SingleAsync();
        var events = await auditDatabase.AuditEvents.ToListAsync();
        Assert.All(events, item => Assert.Equal(account.Id, item.ActorId));
        Assert.All(events, item => Assert.Null(item.OrganizationId));
        Assert.Equal(lockAccount ? 6 : 1, events.Count);
        Assert.Contains(events, item => item.Action == (lockAccount ? "LoginLockedOut" : "LoginSucceeded"));

        if (!lockAccount)
        {
            await RefreshToken(client);
            using var logout = await client.PostAsync("/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            using var signedOut = await client.PostAsync("/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        }
    }

    [Fact]
    public async Task PasswordResetChangesPasswordAndInvalidatesExistingSession()
    {
        var delivery = new CapturedResetDelivery();
        using var host = CreateHost(delivery);
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        const string email = "reset@example.test";
        var original = $"Old!1{Guid.NewGuid():N}";
        var replacement = $"New!1{Guid.NewGuid():N}";
        await RefreshToken(client);

        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = original });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = original });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        await RefreshToken(client);

        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "unknown@example.test" });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Null(delivery.Code);
        using var request = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.NotNull(delivery.Code);

        var reset = new { email, code = delivery.Code, newPassword = replacement };
        using var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var oldSession = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        await RefreshToken(client);
        using var replay = await client.PostAsJsonAsync("/api/v1/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using var oldPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = original });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        using var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = replacement });
        Assert.Equal(HttpStatusCode.NoContent, newLogin.StatusCode);
    }

    [Fact]
    public async Task OrganizationCreationAndInvitationsEnforceMembershipRules()
    {
        using var host = CreateHost();
        await VerifyOrganizationFlow(host);
    }

    [SqlServerFact]
    public async Task OrganizationIsolationOnSqlServer()
    {
        var source = Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionVariable);
        var connection = new SqlConnectionStringBuilder(source)
        {
            InitialCatalog = $"BanesaHub_Test_{Guid.NewGuid():N}",
            Pooling = false
        };
        using var host = CreateHost(sqlServer: connection.ConnectionString);
        await using var scope = host.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BanesaHubDbContext>();

        try
        {
            await database.Database.MigrateAsync();
            await VerifyOrganizationFlow(host);
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }

    private static async Task VerifyOrganizationFlow(WebApplicationFactory<Program> host)
    {
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        const string route = "/api/v1/organizations";
        var organization = new { name = " Example Rentals ", slug = "EXAMPLE-RENTALS" };
        using var anonymous = await client.PostAsJsonAsync(route, organization);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        await RefreshToken(client);
        const string email = "owner@example.test";
        var credentials = new { email, password = $"Test!1{Guid.NewGuid():N}" };
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register", credentials);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", credentials);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        using var staleToken = await client.PostAsJsonAsync(route, organization);
        Assert.Equal(HttpStatusCode.BadRequest, staleToken.StatusCode);
        await RefreshToken(client);

        using var created = await client.PostAsJsonAsync(route, organization);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var duplicate = await client.PostAsJsonAsync(route, new { name = "Duplicate", slug = "example-rentals" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var invalid = await client.PostAsJsonAsync(route, new { name = "Invalid", slug = "invalid slug" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<BanesaHubDbContext>();
        var user = await database.Users.SingleAsync();
        var saved = await database.Organizations.SingleAsync();
        var membership = await database.Memberships.SingleAsync();
        Assert.Equal("Example Rentals", saved.Name);
        Assert.Equal("example-rentals", saved.Slug);
        Assert.Equal(saved.Id, membership.OrganizationId);
        Assert.Equal(user.Id, membership.UserId);
        Assert.Equal(BanesaHub.Domain.Organizations.MembershipRole.Owner, membership.Role);

        var organizationRoute = $"/api/v1/organizations/{saved.Id}";
        using var ownerAccess = await client.GetAsync(organizationRoute);
        Assert.Equal(HttpStatusCode.OK, ownerAccess.StatusCode);

        var inviteRoute = $"/api/v1/organizations/{saved.Id}/invitations";
        const string inviteeEmail = "invitee@example.test";
        using var issue = await client.PostAsJsonAsync(inviteRoute, new { email = inviteeEmail, role = 2 });
        Assert.Equal(HttpStatusCode.Created, issue.StatusCode);
        var issued = await issue.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(issued);

        const string acceptRoute = "/api/v1/organizations/invitations/accept";
        var acceptance = new { token = issued.Token };
        using var wrongAccount = await client.PostAsJsonAsync(acceptRoute, acceptance);
        Assert.Equal(HttpStatusCode.BadRequest, wrongAccount.StatusCode);

        using var invitee = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        await RefreshToken(invitee);
        var inviteeCredentials = new { email = inviteeEmail, password = credentials.password };
        using var inviteeRegister = await invitee.PostAsJsonAsync("/api/v1/auth/register", inviteeCredentials);
        Assert.Equal(HttpStatusCode.Created, inviteeRegister.StatusCode);
        using var inviteeLogin = await invitee.PostAsJsonAsync("/api/v1/auth/login", inviteeCredentials);
        Assert.Equal(HttpStatusCode.NoContent, inviteeLogin.StatusCode);
        await RefreshToken(invitee);
        using var outsiderAccess = await invitee.GetAsync(organizationRoute);
        Assert.Equal(HttpStatusCode.NotFound, outsiderAccess.StatusCode);
        using var unauthorizedIssue = await invitee.PostAsJsonAsync(inviteRoute, new { email = inviteeEmail, role = 2 });
        Assert.Equal(HttpStatusCode.NotFound, unauthorizedIssue.StatusCode);
        using var accepted = await invitee.PostAsJsonAsync(acceptRoute, acceptance);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        using var memberAccess = await invitee.GetAsync(organizationRoute);
        Assert.Equal(HttpStatusCode.OK, memberAccess.StatusCode);
        using var replay = await invitee.PostAsJsonAsync(acceptRoute, acceptance);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        var inviteeId = await database.Users.Where(item => item.Email == inviteeEmail).Select(item => item.Id).SingleAsync();
        var inviteeMembership = await database.Memberships.SingleAsync(item => item.UserId == inviteeId);
        Assert.Equal(saved.Id, inviteeMembership.OrganizationId);
        Assert.Equal(BanesaHub.Domain.Organizations.MembershipRole.PropertyManager, inviteeMembership.Role);
        using var secondOrg = await invitee.PostAsJsonAsync("/api/v1/organizations", new { name = "Other Rentals", slug = "other-rentals" });
        Assert.Equal(HttpStatusCode.Created, secondOrg.StatusCode);
        var second = await secondOrg.Content.ReadFromJsonAsync<CreatedOrganization>();
        Assert.NotNull(second);
        var inventoryRoute = $"/api/v1/organizations/{saved.Id}/properties";
        using var property = await client.PostAsJsonAsync(inventoryRoute, new { name = "Draft property" });
        Assert.Equal(HttpStatusCode.Created, property.StatusCode);
        var detailRoute = property.Headers.Location!.PathAndQuery;
        var propertyId = Guid.Parse(detailRoute.Split('/')[^1]);
        using var managerAccess = await invitee.GetAsync(detailRoute);
        Assert.Equal(HttpStatusCode.OK, managerAccess.StatusCode);
        using var foreignAccess = await client.GetAsync($"/api/v1/organizations/{second.Id}/properties");
        Assert.Equal(HttpStatusCode.NotFound, foreignAccess.StatusCode);
        using var wrongParent = await invitee.PutAsJsonAsync(
            $"/api/v1/organizations/{second.Id}/properties/{propertyId}", new { name = "Invalid move" });
        Assert.Equal(HttpStatusCode.NotFound, wrongParent.StatusCode);
        using var updated = await client.PutAsJsonAsync(detailRoute, new { name = "Updated property" });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var buildingsRoute = $"{detailRoute}/buildings";
        using var invalidBuilding = await client.PostAsJsonAsync(buildingsRoute, new { name = "Invalid", floorCount = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidBuilding.StatusCode);
        using var addedBuilding = await invitee.PostAsJsonAsync(buildingsRoute, new { name = " North Wing ", floorCount = 2 });
        Assert.Equal(HttpStatusCode.Created, addedBuilding.StatusCode);
        var buildingRoute = addedBuilding.Headers.Location!.PathAndQuery;
        using var buildingDetail = await client.GetAsync(buildingRoute);
        Assert.Equal(HttpStatusCode.OK, buildingDetail.StatusCode);
        using var buildingList = await client.GetAsync($"{buildingsRoute}?offset=1&limit=1");
        Assert.Equal(HttpStatusCode.OK, buildingList.StatusCode);
        Assert.Empty((await buildingList.Content.ReadFromJsonAsync<System.Text.Json.JsonElement[]>())!);

        using var otherProperty = await client.PostAsJsonAsync(inventoryRoute, new { name = "Other property" });
        Assert.Equal(HttpStatusCode.Created, otherProperty.StatusCode);
        using var webHost = new WebApplicationFactory<BanesaHub.Web.Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
                services.AddHttpClient<BanesaHub.Web.Inventory.InventoryApi>()
                    .ConfigurePrimaryHttpMessageHandler(() => host.Server.CreateHandler());
            }));
        using var browser = webHost.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var pageRoute = $"/organizations/{saved.Id}/properties";
        using var home = await browser.GetAsync($"/?organizationId={saved.Id}");
        Assert.Equal(pageRoute, home.Headers.Location?.OriginalString);
        var signInHtml = await browser.GetStringAsync(pageRoute);
        Assert.Contains("Hyni për të parë pronat", signInHtml);
        var token = System.Text.RegularExpressions.Regex.Match(
            signInHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        using var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = credentials.email,
            ["password"] = credentials.password,
            ["__RequestVerificationToken"] = token
        });
        using var webLogin = await browser.PostAsync($"{pageRoute}/sign-in", loginForm);
        Assert.Equal(HttpStatusCode.Found, webLogin.StatusCode);
        var filteredHtml = await browser.GetStringAsync($"{pageRoute}?search=Updated&status=Draft");
        Assert.Contains("Updated property", filteredHtml);
        Assert.DoesNotContain("Other property", filteredHtml);
        var filteredOut = await browser.GetStringAsync($"{pageRoute}?search=Updated&status=Active");
        Assert.Contains("Nuk u gjet asnjë pronë.", filteredOut);
        var nextPage = await browser.GetStringAsync($"{pageRoute}?search=Updated&status=Draft&page=2");
        Assert.DoesNotContain("Updated property", nextPage);
        Assert.Contains("search=Updated", nextPage);
        using var foreignPage = await browser.GetAsync($"/organizations/{second.Id}/properties");
        Assert.Equal(HttpStatusCode.NotFound, foreignPage.StatusCode);

        var createRoute = $"{pageRoute}/new";
        var createHtml = await browser.GetStringAsync(createRoute);
        Assert.Contains("Shto pron", createHtml);
        var formToken = System.Text.RegularExpressions.Regex.Match(
            createHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(formToken);

        Task<HttpResponseMessage> SubmitProperty(string route, string name, bool includeToken = true)
        {
            var fields = new Dictionary<string, string> { ["Input.Name"] = name };
            if (includeToken)
            {
                fields["__RequestVerificationToken"] = formToken;
            }
            return browser.PostAsync(route, new FormUrlEncodedContent(fields));
        }

        using var noCsrf = await SubmitProperty(createRoute, "Rejected", includeToken: false);
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var missingName = await SubmitProperty(createRoute, "");
        Assert.Equal(HttpStatusCode.OK, missingName.StatusCode);
        Assert.Contains("Shkruani emrin", await missingName.Content.ReadAsStringAsync());
        using var webCreated = await SubmitProperty(createRoute, "MVC draft");
        Assert.Equal(HttpStatusCode.Found, webCreated.StatusCode);
        var mvcProperty = await database.Properties.AsNoTracking().SingleAsync(item => item.Name == "MVC draft");
        Assert.Equal(BanesaHub.Domain.Properties.PropertyStatus.Draft, mvcProperty.Status);
        Assert.Null(mvcProperty.Address);
        var editRoute = $"{pageRoute}/{mvcProperty.Id}/edit";
        Assert.Contains("MVC draft", await browser.GetStringAsync(editRoute));
        using var foreignEdit = await SubmitProperty($"/organizations/{second.Id}/properties/{mvcProperty.Id}/edit", "Rejected");
        Assert.Equal(HttpStatusCode.NotFound, foreignEdit.StatusCode);
        using var webEdited = await SubmitProperty(editRoute, "MVC updated");
        Assert.Equal(HttpStatusCode.Found, webEdited.StatusCode);
        var confirmRoute = $"{pageRoute}/{mvcProperty.Id}/archive";
        Assert.Contains("MVC updated", await browser.GetStringAsync(confirmRoute));
        using var webArchived = await SubmitProperty(confirmRoute, "");
        Assert.Equal(HttpStatusCode.Found, webArchived.StatusCode);
        var archivedThroughMvc = await database.Properties.AsNoTracking().SingleAsync(item => item.Id == mvcProperty.Id);
        Assert.Equal("MVC updated", archivedThroughMvc.Name);
        Assert.Equal(BanesaHub.Domain.Properties.PropertyStatus.Archived, archivedThroughMvc.Status);
        Assert.DoesNotContain("MVC updated", await browser.GetStringAsync(pageRoute));

        var webBuildings = $"{pageRoute}/{propertyId}/buildings";
        var webNewBuilding = $"{webBuildings}/new";
        var propertyListHtml = await browser.GetStringAsync(pageRoute);
        Assert.Contains($"href=\"{webBuildings}\"", propertyListHtml);
        Assert.Contains("North Wing", await browser.GetStringAsync(webBuildings));
        Assert.DoesNotContain("North Wing", await browser.GetStringAsync($"{webBuildings}?page=2"));
        var buildingFormHtml = await browser.GetStringAsync(webNewBuilding);
        Assert.Contains($"action=\"{webNewBuilding}\"", buildingFormHtml);

        async Task<HttpResponseMessage> SubmitBuilding(string route, string name, string floors)
        {
            using var fields = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Name"] = name,
                ["Input.FloorCount"] = floors,
                ["__RequestVerificationToken"] = formToken
            });
            return await browser.PostAsync(route, fields);
        }

        using var invalidFloors = await SubmitBuilding(webNewBuilding, "MVC building", "0");
        Assert.Equal(HttpStatusCode.OK, invalidFloors.StatusCode);
        Assert.Contains("duhet t", await invalidFloors.Content.ReadAsStringAsync());
        using var webBuildingCreated = await SubmitBuilding(webNewBuilding, "MVC building", "");
        Assert.Equal(HttpStatusCode.Found, webBuildingCreated.StatusCode);
        var mvcBuilding = await database.Buildings.AsNoTracking().SingleAsync(item => item.PropertyId == propertyId && item.Name == "MVC building");
        Assert.Null(mvcBuilding.FloorCount);
        var otherPropertyId = Guid.Parse(otherProperty.Headers.Location!.AbsolutePath.Split('/')[^1]);
        using var mismatchedEdit = await SubmitBuilding(
            $"/organizations/{saved.Id}/properties/{otherPropertyId}/buildings/{mvcBuilding.Id}/edit", "Rejected", "2");
        Assert.Equal(HttpStatusCode.NotFound, mismatchedEdit.StatusCode);
        var webBuildingEdit = $"{webBuildings}/{mvcBuilding.Id}/edit";
        Assert.Contains("MVC building", await browser.GetStringAsync(webBuildingEdit));
        using var webBuildingUpdated = await SubmitBuilding(webBuildingEdit, "MVC building updated", "4");
        Assert.Equal(HttpStatusCode.Found, webBuildingUpdated.StatusCode);
        var webBuildingArchive = $"{webBuildings}/{mvcBuilding.Id}/archive";
        Assert.Contains("MVC building updated", await browser.GetStringAsync(webBuildingArchive));
        using var webBuildingArchived = await SubmitBuilding(webBuildingArchive, "", "");
        Assert.Equal(HttpStatusCode.Found, webBuildingArchived.StatusCode);
        var archivedMvcBuilding = await database.Buildings.AsNoTracking().SingleAsync(item => item.Id == mvcBuilding.Id);
        Assert.Equal(4, archivedMvcBuilding.FloorCount);
        Assert.True(archivedMvcBuilding.IsArchived);
        Assert.DoesNotContain("MVC building updated", await browser.GetStringAsync(webBuildings));

        var buildingId = buildingRoute.Split('/')[^1];
        using var mismatchedBuilding = await client.GetAsync($"{otherProperty.Headers.Location!.PathAndQuery}/buildings/{buildingId}");
        Assert.Equal(HttpStatusCode.NotFound, mismatchedBuilding.StatusCode);
        using var foreignBuilding = await invitee.GetAsync($"/api/v1/organizations/{second.Id}/properties/{propertyId}/buildings/{buildingId}");
        Assert.Equal(HttpStatusCode.NotFound, foreignBuilding.StatusCode);

        using var changedBuilding = await client.PutAsJsonAsync(buildingRoute, new { name = "North Hall", floorCount = 3 });
        Assert.Equal(HttpStatusCode.NoContent, changedBuilding.StatusCode);
        using var activeBuilding = await client.PostAsJsonAsync(buildingsRoute, new { name = "South Wing" });
        Assert.Equal(HttpStatusCode.Created, activeBuilding.StatusCode);
        var unitsRoute = $"{buildingRoute}/units";
        var otherUnitsRoute = $"{activeBuilding.Headers.Location!.PathAndQuery}/units";

        using var invalidArea = await client.PostAsJsonAsync(unitsRoute, new { number = "A01", area = 1.001m });
        Assert.Equal(HttpStatusCode.BadRequest, invalidArea.StatusCode);
        using var invalidStatus = await client.PostAsJsonAsync(unitsRoute, new { number = "A01", status = 99 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        using var createdUnit = await invitee.PostAsJsonAsync(unitsRoute, new { number = " A01 ", area = 45.50m });
        Assert.Equal(HttpStatusCode.Created, createdUnit.StatusCode);
        var unitRoute = createdUnit.Headers.Location!.PathAndQuery;
        var unitId = Guid.Parse(unitRoute.Split('/')[^1]);
        using var duplicateUnit = await client.PostAsJsonAsync(unitsRoute, new { number = "A01" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateUnit.StatusCode);
        using var sameNumberElsewhere = await client.PostAsJsonAsync(otherUnitsRoute, new { number = "A01" });
        Assert.Equal(HttpStatusCode.Created, sameNumberElsewhere.StatusCode);
        using var wrongBuilding = await client.PutAsJsonAsync($"{otherUnitsRoute}/{unitId}", new { number = "B01" });
        Assert.Equal(HttpStatusCode.NotFound, wrongBuilding.StatusCode);
        using var wrongUnitOrganization = await invitee.GetAsync(
            $"/api/v1/organizations/{second.Id}/properties/{propertyId}/buildings/{buildingId}/units/{unitId}");
        Assert.Equal(HttpStatusCode.NotFound, wrongUnitOrganization.StatusCode);
        using var secondUnit = await client.PostAsJsonAsync(unitsRoute, new { number = "A02" });
        Assert.Equal(HttpStatusCode.Created, secondUnit.StatusCode);
        using var duplicateUpdate = await client.PutAsJsonAsync(unitRoute, new { number = "A02" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateUpdate.StatusCode);
        using var updatedUnit = await client.PutAsJsonAsync(unitRoute, new { number = "A01", area = 46.75m, status = 3 });
        Assert.Equal(HttpStatusCode.NoContent, updatedUnit.StatusCode);
        using var unitDetail = await invitee.GetAsync(unitRoute);
        Assert.Equal(HttpStatusCode.OK, unitDetail.StatusCode);
        using var unitPage = await client.GetAsync($"{unitsRoute}?offset=1&limit=1");
        Assert.Equal(HttpStatusCode.OK, unitPage.StatusCode);
        var page = Assert.Single((await unitPage.Content.ReadFromJsonAsync<System.Text.Json.JsonElement[]>())!);
        Assert.Equal("A02", page.GetProperty("number").GetString());

        var expectedNumbers = Enumerable.Range(1, 10).Select(index => $"A{index:D2}").ToArray();
        foreach (var number in expectedNumbers.Skip(2))
        {
            using var added = await client.PostAsJsonAsync(unitsRoute, new { number, status = 2 });
            Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        }

        var acceptedUnits = await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>(unitsRoute);
        Assert.NotNull(acceptedUnits);
        Assert.Equal(expectedNumbers, acceptedUnits.Select(unit => unit.GetProperty("number").GetString()));
        Assert.Equal(new[] { 3, 1 }.Concat(Enumerable.Repeat(2, 8)),
            acceptedUnits.Select(unit => unit.GetProperty("status").GetInt32()));
        using var duplicateTenth = await client.PostAsJsonAsync(unitsRoute, new { number = "A10" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateTenth.StatusCode);

        var otherOrganizationProperties = $"/api/v1/organizations/{second.Id}/properties";
        using var separateProperty = await invitee.PostAsJsonAsync(otherOrganizationProperties, new { name = "Separate inventory" });
        Assert.Equal(HttpStatusCode.Created, separateProperty.StatusCode);
        using var separateBuilding = await invitee.PostAsJsonAsync(
            $"{separateProperty.Headers.Location!.PathAndQuery}/buildings", new { name = "Separate building" });
        Assert.Equal(HttpStatusCode.Created, separateBuilding.StatusCode);
        var separateUnits = $"{separateBuilding.Headers.Location!.PathAndQuery}/units";
        using var separateUnit = await invitee.PostAsJsonAsync(separateUnits, new { number = "A10" });
        Assert.Equal(HttpStatusCode.Created, separateUnit.StatusCode);
        var separateUnitRoute = separateUnit.Headers.Location!.PathAndQuery;
        using var deniedRead = await client.GetAsync(separateUnitRoute);
        Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
        using var deniedWrite = await client.PutAsJsonAsync(separateUnitRoute, new { number = "Changed outside organization" });
        Assert.Equal(HttpStatusCode.NotFound, deniedWrite.StatusCode);
        var untouchedUnit = await invitee.GetFromJsonAsync<System.Text.Json.JsonElement>(separateUnitRoute);
        Assert.Equal("A10", untouchedUnit.GetProperty("number").GetString());

        var draftHtml = await browser.GetStringAsync($"{pageRoute}?status=Draft&search=Updated");
        Assert.Contains("Updated property</th>", draftHtml);
        Assert.Contains(">Draft</span>", draftHtml);
        var webUnits = $"{webBuildings}/{buildingId}/units";
        Assert.Contains($"href=\"{webUnits}\"", await browser.GetStringAsync(webBuildings));
        var unitPageHtml = await browser.GetStringAsync(webUnits);
        Assert.Contains("46,75", unitPageHtml);
        var decodedUnits = WebUtility.HtmlDecode(unitPageHtml);
        string RenderedUnitRow(string number) => System.Text.RegularExpressions.Regex.Match(decodedUnits,
            $"""(?s)<th scope="row">{number}</th>.*?</tr>""").Value;

        foreach (var number in expectedNumbers)
        {
            Assert.NotEmpty(RenderedUnitRow(number));
        }

        Assert.Contains("E padisponueshme", RenderedUnitRow("A01"));
        Assert.Contains("E lirë", RenderedUnitRow("A02"));
        Assert.Contains("E zënë", RenderedUnitRow("A10"));
        Assert.DoesNotContain("A02", await browser.GetStringAsync($"{webUnits}?page=2"));
        var newUnit = $"{webUnits}/new";
        Assert.Contains($"action=\"{newUnit}\"", await browser.GetStringAsync(newUnit));

        async Task<HttpResponseMessage> SubmitUnit(string url, string number, string area)
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Number"] = number,
                ["Input.Area"] = area,
                ["Input.Status"] = "2",
                ["__RequestVerificationToken"] = formToken
            });
            return await browser.PostAsync(url, form);
        }

        using var invalidUnitArea = await SubmitUnit(newUnit, "MVC-01", "25,555");
        Assert.Equal(HttpStatusCode.OK, invalidUnitArea.StatusCode);
        Assert.Contains("pas presjes", await invalidUnitArea.Content.ReadAsStringAsync());
        using var webDuplicate = await SubmitUnit(newUnit, "A01", "25,55");
        Assert.Equal(HttpStatusCode.Conflict, webDuplicate.StatusCode);
        Assert.Contains("në përdorim", WebUtility.HtmlDecode(await webDuplicate.Content.ReadAsStringAsync()));
        using var webUnitCreated = await SubmitUnit(newUnit, "MVC-01", "25,55");
        Assert.Equal(HttpStatusCode.Found, webUnitCreated.StatusCode);
        var mvcUnit = await database.Units.AsNoTracking().SingleAsync(item => item.Number == "MVC-01");
        Assert.Equal(25.55m, mvcUnit.Area);
        Assert.Equal(BanesaHub.Domain.Properties.UnitStatus.Occupied, mvcUnit.Status);
        var wrongUnitRoute = $"{webBuildings}/{activeBuilding.Headers.Location!.AbsolutePath.Split('/')[^1]}/units/{mvcUnit.Id}/edit";
        using var wrongUnitEdit = await SubmitUnit(wrongUnitRoute, "Rejected", "");
        Assert.Equal(HttpStatusCode.NotFound, wrongUnitEdit.StatusCode);
        var editUnit = $"{webUnits}/{mvcUnit.Id}/edit";
        Assert.Contains("25,55", await browser.GetStringAsync(editUnit));
        using var webUnitUpdated = await SubmitUnit(editUnit, "MVC-02", "26.5");
        Assert.Equal(HttpStatusCode.Found, webUnitUpdated.StatusCode);
        var archiveUnit = $"{webUnits}/{mvcUnit.Id}/archive";
        Assert.Contains("MVC-02", await browser.GetStringAsync(archiveUnit));
        using var webUnitArchived = await SubmitUnit(archiveUnit, "", "");
        Assert.Equal(HttpStatusCode.Found, webUnitArchived.StatusCode);
        Assert.DoesNotContain("MVC-02", await browser.GetStringAsync(webUnits));

        await VerifyLeaseActivation(database, client, invitee, saved.Id, second.Id,
            Guid.Parse(sameNumberElsewhere.Headers.Location!.AbsolutePath.Split('/')[^1]));

        using var archivedUnit = await client.DeleteAsync(unitRoute);
        Assert.Equal(HttpStatusCode.NoContent, archivedUnit.StatusCode);
        using var hiddenUnit = await client.GetAsync(unitRoute);
        Assert.Equal(HttpStatusCode.NotFound, hiddenUnit.StatusCode);
        using var reservedNumber = await client.PostAsJsonAsync(unitsRoute, new { number = "A01" });
        Assert.Equal(HttpStatusCode.Conflict, reservedNumber.StatusCode);
        var storedUnit = await database.Units.SingleAsync(item => item.Id == unitId);
        Assert.Equal("A01", storedUnit.Number);
        Assert.Equal(46.75m, storedUnit.Area);
        Assert.Equal(BanesaHub.Domain.Properties.UnitStatus.Unavailable, storedUnit.Status);
        Assert.True(storedUnit.IsArchived);

        using var archivedBuilding = await client.DeleteAsync(buildingRoute);
        Assert.Equal(HttpStatusCode.NoContent, archivedBuilding.StatusCode);
        using var hiddenBuilding = await client.GetAsync(buildingRoute);
        Assert.Equal(HttpStatusCode.NotFound, hiddenBuilding.StatusCode);
        var storedBuilding = await database.Buildings.SingleAsync(item => item.Id == Guid.Parse(buildingId));
        Assert.Equal("North Hall", storedBuilding.Name);
        Assert.Equal(3, storedBuilding.FloorCount);
        Assert.True(storedBuilding.IsArchived);

        using var hiddenByBuilding = await client.GetAsync(secondUnit.Headers.Location!.PathAndQuery);
        Assert.Equal(HttpStatusCode.NotFound, hiddenByBuilding.StatusCode);
        using var rejectedByBuilding = await client.PostAsJsonAsync(unitsRoute, new { number = "A03" });
        Assert.Equal(HttpStatusCode.NotFound, rejectedByBuilding.StatusCode);

        using var archived = await client.DeleteAsync(detailRoute);
        Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode);
        using var hidden = await client.GetAsync(detailRoute);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var hiddenByParent = await client.GetAsync(activeBuilding.Headers.Location!.PathAndQuery);
        Assert.Equal(HttpStatusCode.NotFound, hiddenByParent.StatusCode);
        using var rejectedByParent = await client.PostAsJsonAsync(buildingsRoute, new { name = "Blocked" });
        Assert.Equal(HttpStatusCode.NotFound, rejectedByParent.StatusCode);
        using var unitHiddenByProperty = await client.GetAsync(sameNumberElsewhere.Headers.Location!.PathAndQuery);
        Assert.Equal(HttpStatusCode.NotFound, unitHiddenByProperty.StatusCode);
        var stored = await database.Properties.SingleAsync(item => item.Id == propertyId);
        Assert.Equal("Updated property", stored.Name);
        Assert.Equal(BanesaHub.Domain.Properties.PropertyStatus.Archived, stored.Status);

        var roleGrants = await database.AuditEvents.Where(item => item.OrganizationId == saved.Id).ToListAsync();
        Assert.Equal(2, roleGrants.Count);
        Assert.Contains(roleGrants, item => item.ActorId == user.Id && item.Action == "MembershipRoleGranted.Owner");
        Assert.Contains(roleGrants, item => item.ActorId == inviteeId && item.Action == "MembershipRoleGranted.PropertyManager");
    }

    private static async Task VerifyLeaseActivation(BanesaHubDbContext database, HttpClient owner,
        HttpClient manager, Guid organizationId, Guid otherOrganizationId, Guid unitId)
    {
        var tenant = new BanesaHub.Domain.Tenants.Tenant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FullName = "Lease test tenant"
        };
        database.Tenants.Add(tenant);
        await database.SaveChangesAsync();
        var route = $"/api/v1/organizations/{organizationId}/leases";

        async Task<Guid> CreateDraft(DateOnly start, DateOnly? end)
        {
            using var response = await owner.PostAsJsonAsync(route, new
            {
                unitId,
                tenantId = tenant.Id,
                startDate = start,
                endDate = end,
                monthlyRent = 325.50m,
                deposit = 300m
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            return body.GetProperty("id").GetGuid();
        }

        var start = new DateOnly(2027, 1, 1);
        var end = new DateOnly(2027, 1, 31);
        var first = await CreateDraft(start, end);
        var version = await database.LeaseVersions.AsNoTracking().SingleAsync(item => item.LeaseId == first);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(start, version.EffectiveFrom);
        Assert.Equal(325.50m, version.MonthlyRent);
        Assert.Equal(300m, version.Deposit);

        using var invalid = await owner.PostAsJsonAsync(route, new
        {
            unitId, tenantId = tenant.Id, startDate = end, endDate = start, monthlyRent = 10.001m
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var wrongScope = await manager.PostAsync(
            $"/api/v1/organizations/{otherOrganizationId}/leases/{first}/activate", null);
        Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);

        if (!database.Database.IsSqlServer())
        {
            return;
        }

        var rival = await CreateDraft(start, end);
        var attempts = await Task.WhenAll(
            owner.PostAsync($"{route}/{first}/activate", null),
            manager.PostAsync($"{route}/{rival}/activate", null));
        using var firstResult = attempts[0];
        using var rivalResult = attempts[1];
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.Conflict);

        var winner = firstResult.StatusCode == HttpStatusCode.NoContent ? first : rival;
        using var retry = await owner.PostAsync($"{route}/{winner}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);

        var boundary = await CreateDraft(end, null);
        using var sameEndDay = await owner.PostAsync($"{route}/{boundary}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, sameEndDay.StatusCode);

        var next = await CreateDraft(end.AddDays(1), null);
        using var nextDay = await owner.PostAsync($"{route}/{next}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, nextDay.StatusCode);

        var later = await CreateDraft(end.AddMonths(1), null);
        using var afterOpenEnded = await owner.PostAsync($"{route}/{later}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, afterOpenEnded.StatusCode);

        Assert.Equal(2, await database.Leases.CountAsync(item =>
            item.OrganizationId == organizationId && item.UnitId == unitId &&
            item.Status == BanesaHub.Domain.Tenants.LeaseStatus.Active));
    }

    private WebApplicationFactory<Program> CreateHost(IPasswordResetDelivery? delivery = null, string? sqlServer = null)
    {
        var databaseName = Guid.NewGuid().ToString();
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
            services.RemoveAll<DbContextOptions<BanesaHubDbContext>>();
            services.AddScoped(_ =>
            {
                var options = new DbContextOptionsBuilder<BanesaHubDbContext>();
                return sqlServer is null
                    ? options.UseInMemoryDatabase(databaseName).Options
                    : options.UseSqlServer(sqlServer).Options;
            });
            if (delivery is not null)
            {
                services.AddSingleton(delivery);
            }
        }));
    }

    private sealed class CapturedResetDelivery : IPasswordResetDelivery
    {
        public bool IsConfigured => true;
        public string? Code { get; private set; }

        public Task SendAsync(string recipient, string token, CancellationToken cancellationToken)
        {
            Code = token;
            return Task.CompletedTask;
        }
    }

    private static async Task RefreshToken(HttpClient client)
    {
        using var result = await client.GetAsync("/api/v1/auth/antiforgery");
        Assert.True(result.IsSuccessStatusCode, await result.Content.ReadAsStringAsync());
        var response = await result.Content.ReadFromJsonAsync<AntiforgeryResponse>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response!.RequestToken);
    }

    private sealed record CreatedOrganization(Guid Id);

    private sealed record InvitationResponse(string Token);

    private sealed record AntiforgeryResponse(string RequestToken);
}
