using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BanesaHub.IntegrationTests;

public sealed class ApiSmokeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthReturnsHealthy()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task OpenApiIsAvailableOnlyInDevelopment(string environment, HttpStatusCode expectedStatus)
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(expectedStatus, response.StatusCode);
        if (expectedStatus == HttpStatusCode.OK)
        {
            await using var body = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(body);
            Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString());
        }
    }
}
