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
        var roleGrants = await database.AuditEvents.Where(item => item.OrganizationId == saved.Id).ToListAsync();
        Assert.Equal(2, roleGrants.Count);
        Assert.Contains(roleGrants, item => item.ActorId == user.Id && item.Action == "MembershipRoleGranted.Owner");
        Assert.Contains(roleGrants, item => item.ActorId == inviteeId && item.Action == "MembershipRoleGranted.PropertyManager");
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

    private sealed record InvitationResponse(string Token);

    private sealed record AntiforgeryResponse(string RequestToken);
}
