using System.Text.Json;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class ApiContractSnapshotTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        ".."));

    private readonly HttpClient client;

    public ApiContractSnapshotTests(ApiWebApplicationFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_MaintainsStableCoreApiSurfaceSnapshot()
    {
        using var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
        swaggerResponse.EnsureSuccessStatusCode();

        using var swagger = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStringAsync());
        using var snapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "tests", "OpenJobEngine.Api.Tests", "Contract", "api-v1-core-surface.json")));

        var swaggerPaths = swagger.RootElement.GetProperty("paths");
        var requiredPaths = snapshot.RootElement.GetProperty("requiredPaths");

        foreach (var pathEntry in requiredPaths.EnumerateObject())
        {
            Assert.True(swaggerPaths.TryGetProperty(pathEntry.Name, out var operations), $"Missing path in swagger snapshot verification: {pathEntry.Name}");

            foreach (var method in pathEntry.Value.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                Assert.True(
                    operations.TryGetProperty(method!, out _),
                    $"Missing method '{method}' for path '{pathEntry.Name}' in swagger surface.");
            }
        }
    }
}
