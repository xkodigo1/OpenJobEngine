using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Collections;

public sealed class JobCollectionService(
    IEnumerable<IJobProvider> providers,
    INormalizationService normalizationService,
    IJobEnrichmentService jobEnrichmentService,
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
                var seenObservationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in rawJobs)
                {
                    var observedAtUtc = DateTimeOffset.UtcNow;
                    seenObservationKeys.Add(BuildObservationKey(raw.SourceName, raw.SourceJobId));

                    var dedupKey = deduplicationService.BuildKey(raw);
                    var normalized = normalizationService.Normalize(raw);
                    normalized = jobEnrichmentService.Enrich(normalized, raw);
                    normalized.AssignDeduplicationKey(dedupKey);
                    normalized.MarkSeen(observedAtUtc);

                    var existingBySource = await jobRepository.GetBySourceAsync(raw.SourceName, raw.SourceJobId, cancellationToken);
                    var existingByDedup = existingBySource is null
                        ? await jobRepository.GetByDedupKeyAsync(dedupKey, cancellationToken)
                        : null;

                    var isNewJob = false;
                    var job = existingBySource ?? existingByDedup ?? normalized;
                    var wasJobActive = job.IsActive;
                    var previousSnapshot = existingBySource is not null || existingByDedup is not null
                        ? BuildSnapshot(job)
                        : null;

                    if (existingBySource is not null)
                    {
                        existingBySource.RefreshFrom(normalized, dedupKey);
                        existingBySource.MarkSeen(observedAtUtc);
                        await jobRepository.UpdateAsync(existingBySource, cancellationToken);
                        updatedJobs++;
                    }
                    else if (existingByDedup is not null)
                    {
                        existingByDedup.RefreshFrom(normalized, dedupKey, preserveSourceIdentity: true);
                        existingByDedup.MarkSeen(observedAtUtc);
                        await jobRepository.UpdateAsync(existingByDedup, cancellationToken);
                        updatedJobs++;
                        deduplicatedJobs++;
                    }
                    else
                    {
                        isNewJob = true;
                        await jobRepository.AddAsync(normalized, cancellationToken);
                        createdJobs++;
                    }

                    job = existingBySource ?? existingByDedup ?? normalized;
                    var currentSnapshot = BuildSnapshot(job);
                    var currentSnapshotJson = SerializeSnapshot(currentSnapshot);
                    var currentSnapshotHash = ComputeSnapshotHash(currentSnapshotJson);

                    var observation = await jobRepository.GetObservationAsync(raw.SourceName, raw.SourceJobId, cancellationToken);
                    var wasObservationInactive = observation is { IsActive: false };
                    var previousObservationHash = observation?.SnapshotHash;

                    if (observation is null)
                    {
                        observation = new JobOfferSourceObservation(
                            Guid.NewGuid(),
                            job.Id,
                            raw.SourceName,
                            raw.SourceJobId,
                            true,
                            observedAtUtc,
                            observedAtUtc,
                            currentSnapshotHash);
                        await jobRepository.AddObservationAsync(observation, cancellationToken);
                    }
                    else
                    {
                        observation.ReassignJobOffer(job.Id);
                        observation.MarkSeen(observedAtUtc, currentSnapshotHash);
                        await jobRepository.UpdateObservationAsync(observation, cancellationToken);
                    }

                    foreach (var historyEntry in BuildHistoryEntries(
                                 job.Id,
                                 raw.SourceName,
                                 isNewJob,
                                 wasJobActive,
                                 wasObservationInactive,
                                 previousSnapshot,
                                 currentSnapshot,
                                 previousObservationHash,
                                 currentSnapshotHash,
                                 observedAtUtc))
                    {
                        await jobRepository.AddHistoryEntryAsync(historyEntry, cancellationToken);
                    }
                }

                await DeactivateMissingObservationsAsync(provider.SourceName, seenObservationKeys, cancellationToken);

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

    private async Task DeactivateMissingObservationsAsync(
        string sourceName,
        HashSet<string> seenObservationKeys,
        CancellationToken cancellationToken)
    {
        var activeObservations = await jobRepository.GetActiveObservationsBySourceAsync(sourceName, cancellationToken);

        foreach (var observation in activeObservations)
        {
            if (seenObservationKeys.Contains(BuildObservationKey(observation.SourceName, observation.SourceJobId)))
            {
                continue;
            }

            observation.MarkInactive();
            await jobRepository.UpdateObservationAsync(observation, cancellationToken);

            var job = await jobRepository.GetByIdAsync(observation.JobOfferId, cancellationToken);
            if (job is null)
            {
                continue;
            }

            var observations = await jobRepository.GetObservationsByJobIdAsync(job.Id, cancellationToken);
            var remainsActive = observations.Any(x => x.Id != observation.Id && x.IsActive);
            job.SetActiveState(remainsActive);
            await jobRepository.UpdateAsync(job, cancellationToken);

            var snapshot = BuildSnapshot(job);
            var snapshotJson = SerializeSnapshot(snapshot);
            var snapshotHash = ComputeSnapshotHash(snapshotJson);

            await jobRepository.AddHistoryEntryAsync(
                new JobOfferHistoryEntry(
                    Guid.NewGuid(),
                    job.Id,
                    JobOfferHistoryEventType.Deactivated,
                    snapshotHash,
                    snapshotJson,
                    sourceName,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }

    private static IReadOnlyCollection<JobOfferHistoryEntry> BuildHistoryEntries(
        Guid jobId,
        string sourceName,
        bool isNewJob,
        bool wasJobActive,
        bool wasObservationInactive,
        JobOfferSnapshot? previousSnapshot,
        JobOfferSnapshot currentSnapshot,
        string? previousObservationHash,
        string currentSnapshotHash,
        DateTimeOffset occurredAtUtc)
    {
        var entries = new List<JobOfferHistoryEntry>();
        var currentSnapshotJson = SerializeSnapshot(currentSnapshot);

        if (isNewJob)
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.Created,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));

            return entries;
        }

        if (previousSnapshot is null)
        {
            return entries;
        }

        var previousSnapshotJson = SerializeSnapshot(previousSnapshot);
        var previousSnapshotHash = ComputeSnapshotHash(previousSnapshotJson);
        var hasMaterialChanges = !string.Equals(previousSnapshotHash, currentSnapshotHash, StringComparison.Ordinal);
        var hasObservationChange = !string.Equals(previousObservationHash, currentSnapshotHash, StringComparison.Ordinal);

        if ((!wasJobActive && currentSnapshot.IsActive) || wasObservationInactive)
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.Reactivated,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        if (hasMaterialChanges || hasObservationChange)
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.Updated,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        if (previousSnapshot.SalaryMin != currentSnapshot.SalaryMin ||
            previousSnapshot.SalaryMax != currentSnapshot.SalaryMax ||
            !string.Equals(previousSnapshot.SalaryCurrency, currentSnapshot.SalaryCurrency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previousSnapshot.SalaryText, currentSnapshot.SalaryText, StringComparison.OrdinalIgnoreCase))
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.SalaryChanged,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        if (!string.Equals(previousSnapshot.City, currentSnapshot.City, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previousSnapshot.Region, currentSnapshot.Region, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previousSnapshot.CountryCode, currentSnapshot.CountryCode, StringComparison.OrdinalIgnoreCase) ||
            previousSnapshot.WorkMode != currentSnapshot.WorkMode ||
            previousSnapshot.IsRemote != currentSnapshot.IsRemote)
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.LocationChanged,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        if (previousSnapshot.QualityScore != currentSnapshot.QualityScore ||
            !previousSnapshot.QualityFlags.SequenceEqual(currentSnapshot.QualityFlags, StringComparer.OrdinalIgnoreCase))
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.QualityChanged,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        if (!previousSnapshot.SkillSignals.SequenceEqual(currentSnapshot.SkillSignals, StringComparer.OrdinalIgnoreCase) ||
            !previousSnapshot.LanguageSignals.SequenceEqual(currentSnapshot.LanguageSignals, StringComparer.OrdinalIgnoreCase))
        {
            entries.Add(new JobOfferHistoryEntry(
                Guid.NewGuid(),
                jobId,
                JobOfferHistoryEventType.SkillSignalsChanged,
                currentSnapshotHash,
                currentSnapshotJson,
                sourceName,
                occurredAtUtc));
        }

        return entries;
    }

    private static JobOfferSnapshot BuildSnapshot(JobOffer job)
    {
        return new JobOfferSnapshot(
            job.Title,
            job.CompanyName,
            job.SeniorityLevel.ToString(),
            job.WorkMode.ToString(),
            job.City,
            job.Region,
            job.CountryCode,
            job.SalaryText,
            job.SalaryMin,
            job.SalaryMax,
            job.SalaryCurrency,
            job.IsRemote,
            decimal.Round(job.QualityScore, 2),
            job.GetQualityFlags().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            job.SkillTags
                .Select(x => $"{x.SkillSlug}:{x.IsRequired}:{x.ConfidenceScore:0.00}")
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            job.LanguageRequirements
                .Select(x => $"{x.LanguageCode}:{x.MinimumProficiency}:{x.IsRequired}:{x.ConfidenceScore:0.00}")
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            job.IsActive);
    }

    private static string SerializeSnapshot(JobOfferSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot);
    }

    private static string ComputeSnapshotHash(string snapshotJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson));
        return Convert.ToHexString(bytes);
    }

    private static string BuildObservationKey(string sourceName, string sourceJobId)
    {
        return $"{sourceName.Trim().ToLowerInvariant()}::{sourceJobId.Trim().ToLowerInvariant()}";
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

    private sealed record JobOfferSnapshot(
        string Title,
        string CompanyName,
        string SeniorityLevel,
        string WorkMode,
        string? City,
        string? Region,
        string? CountryCode,
        string? SalaryText,
        decimal? SalaryMin,
        decimal? SalaryMax,
        string? SalaryCurrency,
        bool IsRemote,
        decimal QualityScore,
        IReadOnlyCollection<string> QualityFlags,
        IReadOnlyCollection<string> SkillSignals,
        IReadOnlyCollection<string> LanguageSignals,
        bool IsActive);
}
