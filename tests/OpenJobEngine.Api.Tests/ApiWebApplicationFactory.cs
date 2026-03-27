using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OpenJobEngine.Api.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string sqlitePath = Path.Combine(Path.GetTempPath(), $"openjobengine-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "Sqlite",
                ["Persistence:ApplyMigrationsOnStartup"] = "true",
                ["ConnectionStrings:Sqlite"] = $"Data Source={sqlitePath}",
                ["Providers:Computrabajo:Enabled"] = "false",
                ["Providers:Adzuna:Enabled"] = "false",
                ["Providers:Greenhouse:Enabled"] = "false"
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(sqlitePath))
        {
            try
            {
                File.Delete(sqlitePath);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }
    }
}
