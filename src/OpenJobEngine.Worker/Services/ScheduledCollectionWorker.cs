using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Worker.Options;

namespace OpenJobEngine.Worker.Services;

public sealed class ScheduledCollectionWorker(
    IOptions<CollectionWorkerOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduledCollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerOptions = options.Value;

        if (!workerOptions.Enabled)
        {
            logger.LogInformation("Scheduled collection worker is disabled");
            return;
        }

        if (workerOptions.RunOnStartup)
        {
            await RunCollectionAsync(workerOptions, stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, workerOptions.IntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);
            await RunCollectionAsync(workerOptions, stoppingToken);
        }
    }

    private async Task RunCollectionAsync(CollectionWorkerOptions workerOptions, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var jobCollectionService = scope.ServiceProvider.GetRequiredService<IJobCollectionService>();

        if (string.IsNullOrWhiteSpace(workerOptions.SourceName))
        {
            logger.LogInformation("Running scheduled collection for all enabled providers");
            await jobCollectionService.RunAllAsync(cancellationToken);
            return;
        }

        logger.LogInformation("Running scheduled collection for source {SourceName}", workerOptions.SourceName);
        await jobCollectionService.RunSourceAsync(workerOptions.SourceName, cancellationToken);
    }
}
