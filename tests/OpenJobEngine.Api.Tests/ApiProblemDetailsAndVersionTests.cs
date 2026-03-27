using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class ApiProblemDetailsAndVersionTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly string ExpectedVersion = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?
        .Split('+', 2)[0]
        ?? "0.0.0";
    private readonly HttpClient client;

    public ApiProblemDetailsAndVersionTests(ApiWebApplicationFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ExposesCurrentVersion_AndVersionHeader()
    {
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-OpenJobEngine-Version", out var values));
        Assert.Contains(ExpectedVersion, values);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ExpectedVersion, payload.RootElement.GetProperty("info").GetProperty("version").GetString());
    }

    [Fact]
    public async Task Swagger_ExposesCoreApiCompatibilitySurface()
    {
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = payload.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/jobs", out _));
        Assert.True(paths.TryGetProperty("/api/jobs/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/jobs/{id}/match", out _));
        Assert.True(paths.TryGetProperty("/api/profiles", out _));
        Assert.True(paths.TryGetProperty("/api/profiles/{profileId}", out _));
        Assert.True(paths.TryGetProperty("/api/profiles/{profileId}/resume", out _));
        Assert.True(paths.TryGetProperty("/api/matching/search", out _));
        Assert.True(paths.TryGetProperty("/api/metrics/overview", out _));
    }

    [Fact]
    public async Task UnknownCollectionSource_ReturnsProblemDetails()
    {
        var response = await client.PostAsync("/api/collections/run/unknown-source", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Resource not found", payload.RootElement.GetProperty("title").GetString());
        Assert.Contains("unknown-source", payload.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ExpectedVersion, payload.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task InvalidProfilePayload_ReturnsValidationProblemDetails()
    {
        using var response = await client.PostAsync(
            "/api/profiles",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation failed", payload.RootElement.GetProperty("title").GetString());
        Assert.True(payload.RootElement.TryGetProperty("errors", out _));
        Assert.Equal(ExpectedVersion, payload.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task MissingProfile_ReturnsProblemDetailsBody()
    {
        var response = await client.GetAsync($"/api/profiles/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Resource not found", payload.GetProperty("title").GetString());
        Assert.Equal(ExpectedVersion, payload.GetProperty("version").GetString());
    }
}
