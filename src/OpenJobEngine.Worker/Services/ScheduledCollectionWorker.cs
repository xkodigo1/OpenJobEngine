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
            await RunSingleCycleAsync(stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, workerOptions.IntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSingleCycleAsync(stoppingToken);
        }
    }

    public async Task<WorkerCycleSummary> RunSingleCycleAsync(CancellationToken cancellationToken)
    {
        var workerOptions = options.Value;
        var startedAtUtc = DateTimeOffset.UtcNow;

        if (!workerOptions.Enabled)
        {
            return WorkerCycleSummary.Disabled(startedAtUtc);
        }

        var sourceSummaries = await RunScheduledCollectionAsync(workerOptions, cancellationToken);
        var successfulSources = sourceSummaries.Count(x => x.Success);
        var failedSources = sourceSummaries.Count(x => !x.Success && !x.Skipped);
        var skippedSources = sourceSummaries.Count(x => x.Skipped);
        var timedOutSources = sourceSummaries.Count(x => x.TimedOut);
        var alerts = await DispatchAlertsAsync(workerOptions, successfulSources > 0, cancellationToken);

        var summary = new WorkerCycleSummary(
            startedAtUtc,
            DateTimeOffset.UtcNow,
            true,
            sourceSummaries.Count,
            successfulSources,
            failedSources,
            skippedSources,
            timedOutSources,
            sourceSummaries.Sum(x => x.TotalCollected),
            sourceSummaries.Sum(x => x.CreatedJobs),
            sourceSummaries.Sum(x => x.UpdatedJobs),
            sourceSummaries.Sum(x => x.DeduplicatedJobs),
            sourceSummaries.Sum(x => x.DeactivatedJobs),
            alerts is not null,
            alerts,
            sourceSummaries);

        logger.LogInformation(
            "Worker cycle completed. SelectedSources={SelectedSources} SuccessfulSources={SuccessfulSources} FailedSources={FailedSources} SkippedSources={SkippedSources} TimedOutSources={TimedOutSources} TotalCollected={TotalCollected} CreatedJobs={CreatedJobs} UpdatedJobs={UpdatedJobs} DeactivatedJobs={DeactivatedJobs} AlertsDispatched={AlertsDispatched}",
            summary.SelectedSources,
            summary.SuccessfulSources,
            summary.FailedSources,
            summary.SkippedSources,
            summary.TimedOutSources,
            summary.TotalCollected,
            summary.CreatedJobs,
            summary.UpdatedJobs,
            summary.DeactivatedJobs,
            summary.AlertsDispatched);

        return summary;
    }

    private async Task<IReadOnlyCollection<WorkerSourceRunSummary>> RunScheduledCollectionAsync(
        CollectionWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var selectedProviders = ResolveProviders(workerOptions);

        if (selectedProviders.Count == 0)
        {
            logger.LogWarning("No enabled providers are available for scheduled collection");
            return Array.Empty<WorkerSourceRunSummary>();
        }

        if (selectedProviders.Count == 1)
        {
            return [await RunProviderWithPoliciesAsync(selectedProviders[0], workerOptions, cancellationToken)];
        }

        var summaries = new ConcurrentBag<WorkerSourceRunSummary>();
        var maxConcurrency = Math.Max(1, workerOptions.MaxConcurrentSources);
        await Parallel.ForEachAsync(
            selectedProviders,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            },
            async (provider, token) =>
            {
                var summary = await RunProviderWithPoliciesAsync(provider, workerOptions, token);
                summaries.Add(summary);
            });

        return summaries
            .OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<IJobProvider> ResolveProviders(CollectionWorkerOptions workerOptions)
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

    private async Task<WorkerSourceRunSummary> RunProviderWithPoliciesAsync(
        IJobProvider provider,
        CollectionWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var sourceLock = sourceLocks.GetOrAdd(provider.SourceName, _ => new SemaphoreSlim(1, 1));

        if (workerOptions.SkipIfAlreadyRunning && !await sourceLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Skipping scheduled collection for source {SourceName} because a run is already in progress", provider.SourceName);
            return WorkerSourceRunSummary.CreateSkipped(provider.SourceName);
        }

        if (!workerOptions.SkipIfAlreadyRunning)
        {
            await sourceLock.WaitAsync(cancellationToken);
        }

        try
        {
            return await RunProviderWithRetriesAsync(provider.SourceName, workerOptions, cancellationToken);
        }
        finally
        {
            sourceLock.Release();
        }
    }

    private async Task<WorkerSourceRunSummary> RunProviderWithRetriesAsync(
        string sourceName,
        CollectionWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, workerOptions.RetryCount + 1);
        var initialDelay = TimeSpan.FromSeconds(Math.Max(1, workerOptions.RetryInitialDelaySeconds));
        var backoffMultiplier = workerOptions.RetryBackoffMultiplier < 1 ? 1 : workerOptions.RetryBackoffMultiplier;
        var providerTimeout = TimeSpan.FromSeconds(Math.Max(1, workerOptions.ProviderTimeoutSeconds));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobCollectionService = scope.ServiceProvider.GetRequiredService<IJobCollectionService>();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(providerTimeout);

                logger.LogInformation(
                    "Running scheduled collection for source {SourceName} (attempt {Attempt}/{MaxAttempts}, timeout {Timeout})",
                    sourceName,
                    attempt,
                    maxAttempts,
                    providerTimeout);

                var result = await jobCollectionService.RunSourceAsync(sourceName, timeoutCts.Token);
                var sourceSummary = result.Sources.FirstOrDefault(x =>
                    string.Equals(x.SourceName, sourceName, StringComparison.OrdinalIgnoreCase));

                if (sourceSummary is not null && sourceSummary.Success)
                {
                    return WorkerSourceRunSummary.FromCollectionSummary(sourceSummary, attempt, false);
                }

                var errorMessage = sourceSummary?.ErrorMessage ?? "Scheduled collection returned no provider summary.";
                if (attempt < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(backoffMultiplier, attempt - 1));
                    logger.LogWarning(
                        "Scheduled collection for source {SourceName} completed with failure summary on attempt {Attempt}/{MaxAttempts}; retrying in {Delay}. Error={ErrorMessage}",
                        sourceName,
                        attempt,
                        maxAttempts,
                        delay,
                        errorMessage);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return WorkerSourceRunSummary.Failed(sourceName, attempt, false, errorMessage);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                var timedOut = true;
                if (attempt < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(backoffMultiplier, attempt - 1));
                    logger.LogWarning(
                        exception,
                        "Scheduled collection for source {SourceName} timed out on attempt {Attempt}/{MaxAttempts}; retrying in {Delay}",
                        sourceName,
                        attempt,
                        maxAttempts,
                        delay);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                logger.LogError(
                    exception,
                    "Scheduled collection for source {SourceName} timed out after {Attempts} attempts",
                    sourceName,
                    maxAttempts);
                return WorkerSourceRunSummary.Failed(sourceName, attempt, timedOut, $"Timed out after {providerTimeout.TotalSeconds:0} seconds.");
            }
            catch (Exception exception)
            {
                if (attempt < maxAttempts)
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
                    continue;
                }

                logger.LogError(
                    exception,
                    "Scheduled collection for source {SourceName} failed after {Attempts} attempts",
                    sourceName,
                    maxAttempts);
                return WorkerSourceRunSummary.Failed(sourceName, attempt, false, exception.Message);
            }
        }

        return WorkerSourceRunSummary.Failed(sourceName, maxAttempts, false, "Scheduled collection ended unexpectedly.");
    }

    private async Task<WorkerAlertDispatchSummary?> DispatchAlertsAsync(
        CollectionWorkerOptions workerOptions,
        bool anyCollectionSucceeded,
        CancellationToken cancellationToken)
    {
        if (!workerOptions.DispatchAlertsAfterCollection)
        {
            return null;
        }

        if (workerOptions.OnlyDispatchAlertsWhenAnyCollectionSucceeded && !anyCollectionSucceeded)
        {
            logger.LogInformation("Skipping alert dispatch because no collection source completed successfully in the current cycle");
            return null;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var alertDispatchService = scope.ServiceProvider.GetRequiredService<IAlertDispatchService>();
            var result = await alertDispatchService.DispatchActiveAlertsAsync(cancellationToken);

            logger.LogInformation(
                "Alert dispatch completed. EvaluatedAlerts={EvaluatedAlerts} MatchedJobs={MatchedJobs} Delivered={DeliveredCount} Recorded={RecordedCount} Failed={FailedCount} Skipped={SkippedCount}",
                result.EvaluatedAlerts,
                result.MatchedJobs,
                result.DeliveredCount,
                result.RecordedCount,
                result.FailedCount,
                result.SkippedCount);

            return WorkerAlertDispatchSummary.FromDto(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Alert dispatch stage failed after collection cycle");
            return WorkerAlertDispatchSummary.Failed(exception.Message);
        }
    }
}
