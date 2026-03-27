using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class OperationalBaselineTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task HealthEndpoints_ReportLiveAndReady()
    {
        using var client = factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var livePayload = await liveResponse.Content.ReadFromJsonAsync<JsonElement>();
        var readyPayload = await readyResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Healthy", livePayload.GetProperty("status").GetString());
        Assert.Equal("Healthy", readyPayload.GetProperty("status").GetString());
        Assert.Contains(
            readyPayload.GetProperty("checks").EnumerateArray().Select(x => x.GetProperty("name").GetString()),
            name => string.Equals(name, "database", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApiKeySecurity_WhenEnabled_RequiresHeader()
    {
        using var securedFactory = CreateFactoryWithOverrides(new Dictionary<string, string?>
        {
            ["ApiSecurity:Enabled"] = "true",
            ["ApiSecurity:ApiKey"] = "demo-secret"
        });
        using var client = securedFactory.CreateClient();

        var unauthorized = await client.GetAsync("/api/matching/rules");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var authorizedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/matching/rules");
        authorizedRequest.Headers.Add("X-Api-Key", "demo-secret");

        var authorized = await client.SendAsync(authorizedRequest);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    [Fact]
    public async Task CollectionEndpoints_AreRateLimited()
    {
        using var isolatedFactory = CreateFactoryWithOverrides(new Dictionary<string, string?>
        {
            ["ApiSecurity:Enabled"] = "false"
        });
        using var client = isolatedFactory.CreateClient();

        var first = await client.PostAsync("/api/collections/run", content: null);
        var second = await client.PostAsync("/api/collections/run", content: null);
        var third = await client.PostAsync("/api/collections/run", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);

        var payload = await third.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Too many requests", payload.GetProperty("title").GetString());
    }

    private WebApplicationFactory<Program> CreateFactoryWithOverrides(Dictionary<string, string?> overrides)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(overrides);
            });
        });
    }
}
