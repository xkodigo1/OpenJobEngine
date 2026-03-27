using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJobEngine.Infrastructure.Options;
using OpenJobEngine.Infrastructure.Persistence;

namespace OpenJobEngine.Infrastructure.DependencyInjection;

public sealed class OpenJobEngineDatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IOptions<PersistenceOptions> persistenceOptions,
    ILogger<OpenJobEngineDatabaseMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!persistenceOptions.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("Database migrations on startup are disabled");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenJobEngineDbContext>();
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        var hasTables = await databaseCreator.HasTablesAsync(cancellationToken);

        if (hasTables && !appliedMigrations.Any())
        {
            logger.LogWarning(
                "Database contains existing tables but no EF migration history. Skipping automatic migration to avoid conflicting with a schema created outside migrations.");
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("OpenJobEngine database migrations applied");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
