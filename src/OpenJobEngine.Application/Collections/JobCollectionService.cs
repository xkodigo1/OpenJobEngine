using Microsoft.Extensions.Logging;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Collections;

public sealed class JobCollectionService(
    IEnumerable<IJobProvider> providers,
    INormalizationService normalizationService,
    IDeduplicationService deduplicationService,
    IJobRepository jobRepository,
    IScrapeExecutionRepository scrapeExecutionRepository,
    IJobSourceRepository jobSourceRepository,
    IUnitOfWork unitOfWork,
    ILogger<JobCollectionService> logger) : IJobCollectionService
{
    public async Task<CollectionRunResultDto> RunAllAsync(CancellationToken cancellationToken)
    {
        var selectedProviders = providers.OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase).ToArray();
        return await RunInternalAsync(selectedProviders, cancellationToken);
    }

    public async Task<CollectionRunResultDto> RunSourceAsync(string sourceName, CancellationToken cancellationToken)
    {
        var provider = providers.FirstOrDefault(x => string.Equals(x.SourceName, sourceName, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            throw new InvalidOperationException($"No provider registered for source '{sourceName}'.");
        }

        return await RunInternalAsync([provider], cancellationToken);
    }

    public async Task<IReadOnlyCollection<ScrapeExecutionDto>> GetRecentExecutionsAsync(int take, CancellationToken cancellationToken)
    {
        var executions = await scrapeExecutionRepository.GetRecentAsync(take, cancellationToken);
        return executions.Select(ScrapeExecutionDto.FromDomain).ToArray();
    }

    private async Task<CollectionRunResultDto> RunInternalAsync(
        IReadOnlyCollection<IJobProvider> selectedProviders,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var summaries = new List<CollectionRunSummaryDto>(selectedProviders.Count);

        foreach (var provider in selectedProviders)
        {
            var execution = ScrapeExecution.Start(provider.SourceName, DateTimeOffset.UtcNow);
            await scrapeExecutionRepository.AddAsync(execution, cancellationToken);
            await EnsureSourceRegisteredAsync(provider.SourceName, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                var rawJobs = await provider.CollectAsync(cancellationToken);
                var createdJobs = 0;
                var updatedJobs = 0;
                var deduplicatedJobs = 0;

                foreach (var raw in rawJobs)
                {
                    var dedupKey = deduplicationService.BuildKey(raw);
                    var normalized = normalizationService.Normalize(raw);
                    normalized.AssignDeduplicationKey(dedupKey);

                    var existingBySource = await jobRepository.GetBySourceAsync(raw.SourceName, raw.SourceJobId, cancellationToken);

                    if (existingBySource is not null)
                    {
                        existingBySource.RefreshFrom(normalized, dedupKey);
                        await jobRepository.UpdateAsync(existingBySource, cancellationToken);
                        updatedJobs++;
                        continue;
                    }

                    var existingByDedup = await jobRepository.GetByDedupKeyAsync(dedupKey, cancellationToken);

                    if (existingByDedup is not null)
                    {
                        existingByDedup.RefreshFrom(normalized, dedupKey, preserveSourceIdentity: true);
                        await jobRepository.UpdateAsync(existingByDedup, cancellationToken);
                        updatedJobs++;
                        deduplicatedJobs++;
                        continue;
                    }

                    await jobRepository.AddAsync(normalized, cancellationToken);
                    createdJobs++;
                }

                execution.Complete(DateTimeOffset.UtcNow, rawJobs.Count, createdJobs, updatedJobs, deduplicatedJobs);
                await scrapeExecutionRepository.UpdateAsync(execution, cancellationToken);
                await MarkSourceCollectedAsync(provider.SourceName, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                summaries.Add(new CollectionRunSummaryDto(
                    provider.SourceName,
                    rawJobs.Count,
                    createdJobs,
                    updatedJobs,
                    deduplicatedJobs,
                    true,
                    null));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Collection failed for source {SourceName}", provider.SourceName);
                execution.Fail(DateTimeOffset.UtcNow, exception.Message);
                await scrapeExecutionRepository.UpdateAsync(execution, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                summaries.Add(new CollectionRunSummaryDto(
                    provider.SourceName,
                    0,
                    0,
                    0,
                    0,
                    false,
                    exception.Message));
            }
        }

        return new CollectionRunResultDto(startedAtUtc, DateTimeOffset.UtcNow, summaries);
    }

    private async Task EnsureSourceRegisteredAsync(string sourceName, CancellationToken cancellationToken)
    {
        var existingSource = await jobSourceRepository.GetByNameAsync(sourceName, cancellationToken);

        if (existingSource is null)
        {
            var source = new JobSource(Guid.NewGuid(), sourceName, "provider", true, $"Auto-registered source {sourceName}");
            await jobSourceRepository.AddAsync(source, cancellationToken);
            return;
        }

        existingSource.UpdateStatus(true, existingSource.Description);
        await jobSourceRepository.UpdateAsync(existingSource, cancellationToken);
    }

    private async Task MarkSourceCollectedAsync(string sourceName, CancellationToken cancellationToken)
    {
        var source = await jobSourceRepository.GetByNameAsync(sourceName, cancellationToken);
        if (source is null)
        {
            return;
        }

        source.MarkCollected(DateTimeOffset.UtcNow);
        await jobSourceRepository.UpdateAsync(source, cancellationToken);
    }
}
