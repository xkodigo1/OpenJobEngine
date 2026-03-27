using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Worker.Options;

namespace OpenJobEngine.Worker.Services;

public sealed class ScheduledCollectionWorker(
    IOptions<CollectionWorkerOptions> options,
    IServiceScopeFactory scopeFactory,
    IEnumerable<IJobProvider> providers,
    ILogger<ScheduledCollectionWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> sourceLocks = new(StringComparer.OrdinalIgnoreCase);

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
            await RunScheduledCollectionAsync(workerOptions, stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, workerOptions.IntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);
            await RunScheduledCollectionAsync(workerOptions, stoppingToken);
        }
    }

    private async Task RunScheduledCollectionAsync(CollectionWorkerOptions workerOptions, CancellationToken cancellationToken)
    {
        var selectedProviders = ResolveProviders(workerOptions);

        if (selectedProviders.Count == 0)
        {
            logger.LogWarning("No enabled providers are available for scheduled collection");
            return;
        }

        if (selectedProviders.Count == 1)
        {
            await RunProviderWithPoliciesAsync(selectedProviders.First(), workerOptions, cancellationToken);
            return;
        }

        var maxConcurrency = Math.Max(1, workerOptions.MaxConcurrentSources);
        await Parallel.ForEachAsync(
            selectedProviders,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            },
            async (provider, token) => await RunProviderWithPoliciesAsync(provider, workerOptions, token));
    }

    private IReadOnlyCollection<IJobProvider> ResolveProviders(CollectionWorkerOptions workerOptions)
    {
        if (!string.IsNullOrWhiteSpace(workerOptions.SourceName))
        {
            var provider = providers.FirstOrDefault(x =>
                string.Equals(x.SourceName, workerOptions.SourceName, StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                logger.LogWarning("Scheduled collection source {SourceName} is not registered", workerOptions.SourceName);
                return Array.Empty<IJobProvider>();
            }

            return [provider];
        }

        return providers
            .OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task RunProviderWithPoliciesAsync(
        IJobProvider provider,
        CollectionWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var sourceLock = sourceLocks.GetOrAdd(provider.SourceName, _ => new SemaphoreSlim(1, 1));

        if (workerOptions.SkipIfAlreadyRunning && !await sourceLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Skipping scheduled collection for source {SourceName} because a run is already in progress", provider.SourceName);
            return;
        }

        if (!workerOptions.SkipIfAlreadyRunning)
        {
            await sourceLock.WaitAsync(cancellationToken);
        }

        try
        {
            await RunProviderWithRetriesAsync(provider.SourceName, workerOptions, cancellationToken);
        }
        finally
        {
            sourceLock.Release();
        }
    }

    private async Task RunProviderWithRetriesAsync(
        string sourceName,
        CollectionWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, workerOptions.RetryCount + 1);
        var initialDelay = TimeSpan.FromSeconds(Math.Max(1, workerOptions.RetryInitialDelaySeconds));
        var backoffMultiplier = workerOptions.RetryBackoffMultiplier < 1 ? 1 : workerOptions.RetryBackoffMultiplier;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobCollectionService = scope.ServiceProvider.GetRequiredService<IJobCollectionService>();

                logger.LogInformation(
                    "Running scheduled collection for source {SourceName} (attempt {Attempt}/{MaxAttempts})",
                    sourceName,
                    attempt,
                    maxAttempts);

                await jobCollectionService.RunSourceAsync(sourceName, cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(backoffMultiplier, attempt - 1));
                logger.LogWarning(
                    exception,
                    "Scheduled collection for source {SourceName} failed on attempt {Attempt}/{MaxAttempts}; retrying in {Delay}",
                    sourceName,
                    attempt,
                    maxAttempts,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Scheduled collection for source {SourceName} failed after {Attempts} attempts",
                    sourceName,
                    maxAttempts);
                return;
            }
        }
    }
}
