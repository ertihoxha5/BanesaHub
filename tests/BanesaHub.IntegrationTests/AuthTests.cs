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
        var databaseName = Guid.NewGuid().ToString();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
            services.RemoveAll<DbContextOptions<BanesaHubDbContext>>();
            services.AddScoped(_ => new DbContextOptionsBuilder<BanesaHubDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        }));
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

        if (!lockAccount)
        {
            await RefreshToken(client);
            using var logout = await client.PostAsync("/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            using var signedOut = await client.PostAsync("/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
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

    private sealed record AntiforgeryResponse(string RequestToken);
}
