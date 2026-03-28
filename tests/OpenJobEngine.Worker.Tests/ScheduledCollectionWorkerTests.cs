using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Collections;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Worker.Options;
using OpenJobEngine.Worker.Services;
using Xunit;

namespace OpenJobEngine.Worker.Tests;

public sealed class ScheduledCollectionWorkerTests
{
    [Fact]
    public async Task RunSingleCycleAsync_RetriesFailureSummaries_AndSkipsAlertsWhenAllSourcesFail()
    {
        var collectionService = new FakeJobCollectionService();
        collectionService.EnqueueResult(CreateCollectionResult("lever", success: false, totalCollected: 0, errorMessage: "upstream failed"));
        collectionService.EnqueueResult(CreateCollectionResult("lever", success: false, totalCollected: 0, errorMessage: "upstream failed"));
        var alertDispatchService = new FakeAlertDispatchService();
        var worker = CreateWorker(
            new CollectionWorkerOptions
            {
                Enabled = true,
                RetryCount = 1,
                RetryInitialDelaySeconds = 1,
                RetryBackoffMultiplier = 1,
                ProviderTimeoutSeconds = 5,
                DispatchAlertsAfterCollection = true,
                OnlyDispatchAlertsWhenAnyCollectionSucceeded = true
            },
            collectionService,
            alertDispatchService,
            new FakeProvider("lever"));

        var summary = await worker.RunSingleCycleAsync(CancellationToken.None);

        Assert.Equal(2, collectionService.AttemptCount);
        Assert.False(summary.AlertsDispatched);
        Assert.Equal(0, alertDispatchService.DispatchCount);
        Assert.Equal(1, summary.FailedSources);
        Assert.Equal(0, summary.SuccessfulSources);
        Assert.Equal(2, summary.Sources.Single().Attempts);
        Assert.False(summary.Sources.Single().Success);
    }

    [Fact]
    public async Task RunSingleCycleAsync_DispatchesAlertsAfterSuccessfulCollection()
    {
        var collectionService = new FakeJobCollectionService();
        collectionService.EnqueueResult(CreateCollectionResult("greenhouse", success: true, totalCollected: 4, createdJobs: 2, updatedJobs: 1, deactivatedJobs: 1));
        var alertDispatchService = new FakeAlertDispatchService();
        var worker = CreateWorker(
            new CollectionWorkerOptions
            {
                Enabled = true,
                RetryCount = 0,
                ProviderTimeoutSeconds = 5,
                DispatchAlertsAfterCollection = true,
                OnlyDispatchAlertsWhenAnyCollectionSucceeded = true
            },
            collectionService,
            alertDispatchService,
            new FakeProvider("greenhouse"));

        var summary = await worker.RunSingleCycleAsync(CancellationToken.None);

        Assert.Equal(1, collectionService.AttemptCount);
        Assert.True(summary.AlertsDispatched);
        Assert.NotNull(summary.AlertDispatch);
        Assert.True(summary.AlertDispatch!.Success);
        Assert.Equal(1, alertDispatchService.DispatchCount);
        Assert.Equal(1, summary.SuccessfulSources);
        Assert.Equal(4, summary.TotalCollected);
        Assert.Equal(2, summary.CreatedJobs);
        Assert.Equal(1, summary.UpdatedJobs);
        Assert.Equal(1, summary.DeactivatedJobs);
    }

    [Fact]
    public async Task RunSingleCycleAsync_TimesOutLongRunningProviders()
    {
        var collectionService = new FakeJobCollectionService
        {
            Delay = TimeSpan.FromSeconds(2)
        };
        collectionService.EnqueueResult(CreateCollectionResult("adzuna", success: true, totalCollected: 1));
        var alertDispatchService = new FakeAlertDispatchService();
        var worker = CreateWorker(
            new CollectionWorkerOptions
            {
                Enabled = true,
                RetryCount = 0,
                ProviderTimeoutSeconds = 1,
                DispatchAlertsAfterCollection = true,
                OnlyDispatchAlertsWhenAnyCollectionSucceeded = true
            },
            collectionService,
            alertDispatchService,
            new FakeProvider("adzuna"));

        var summary = await worker.RunSingleCycleAsync(CancellationToken.None);

        Assert.Equal(1, collectionService.AttemptCount);
        Assert.Equal(1, summary.TimedOutSources);
        Assert.False(summary.AlertsDispatched);
        Assert.False(summary.Sources.Single().Success);
        Assert.True(summary.Sources.Single().TimedOut);
        Assert.Contains("Timed out", summary.Sources.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ScheduledCollectionWorker CreateWorker(
        CollectionWorkerOptions workerOptions,
        FakeJobCollectionService collectionService,
        FakeAlertDispatchService alertDispatchService,
        params IJobProvider[] providers)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJobCollectionService>(collectionService);
        services.AddSingleton<IAlertDispatchService>(alertDispatchService);

        var serviceProvider = services.BuildServiceProvider();
        return new ScheduledCollectionWorker(
            Microsoft.Extensions.Options.Options.Create(workerOptions),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            providers,
            NullLogger<ScheduledCollectionWorker>.Instance);
    }

    private static CollectionRunResultDto CreateCollectionResult(
        string sourceName,
        bool success,
        int totalCollected,
        int createdJobs = 0,
        int updatedJobs = 0,
        int deduplicatedJobs = 0,
        int deactivatedJobs = 0,
        int staleDeactivatedJobs = 0,
        string? errorMessage = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CollectionRunResultDto(
            now,
            now,
            [
                new CollectionRunSummaryDto(
                    sourceName,
                    totalCollected,
                    createdJobs,
                    updatedJobs,
                    deduplicatedJobs,
                    deactivatedJobs,
                    staleDeactivatedJobs,
                    success,
                    errorMessage)
            ]);
    }

    private sealed class FakeProvider(string sourceName) : IJobProvider
    {
        public string SourceName { get; } = sourceName;

        public Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<RawJobOffer>>(Array.Empty<RawJobOffer>());
        }
    }

    private sealed class FakeJobCollectionService : IJobCollectionService
    {
        private readonly Queue<CollectionRunResultDto> results = new();

        public int AttemptCount { get; private set; }

        public TimeSpan? Delay { get; init; }

        public void EnqueueResult(CollectionRunResultDto result)
        {
            results.Enqueue(result);
        }

        public Task<CollectionRunResultDto> RunAllAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task<CollectionRunResultDto> RunSourceAsync(string sourceName, CancellationToken cancellationToken)
        {
            AttemptCount++;
            if (Delay.HasValue)
            {
                await Task.Delay(Delay.Value, cancellationToken);
            }

            if (results.Count == 0)
            {
                return CreateCollectionResult(sourceName, success: true, totalCollected: 0);
            }

            return results.Dequeue();
        }

        public Task<IReadOnlyCollection<ScrapeExecutionDto>> GetRecentExecutionsAsync(int take, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ScrapeExecutionDto>>(Array.Empty<ScrapeExecutionDto>());
        }
    }

    private sealed class FakeAlertDispatchService : IAlertDispatchService
    {
        public int DispatchCount { get; private set; }

        public Task<AlertDispatchRunDto> DispatchActiveAlertsAsync(CancellationToken cancellationToken)
        {
            DispatchCount++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AlertDispatchRunDto(now, now, 1, 1, 1, 0, 0, 0));
        }
    }
}
