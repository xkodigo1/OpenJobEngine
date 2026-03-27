using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Common;

public sealed class SystemMetricsService(
    IJobRepository jobRepository,
    ICandidateProfileRepository candidateProfileRepository,
    IMatchExecutionRepository matchExecutionRepository,
    IProfileAlertRepository profileAlertRepository,
    IAlertDeliveryRepository alertDeliveryRepository,
    IJobSourceRepository jobSourceRepository,
    IScrapeExecutionRepository scrapeExecutionRepository) : ISystemMetricsService
{
    public async Task<SystemMetricsDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var totalJobs = await jobRepository.CountAsync(false, cancellationToken);
        var activeJobs = await jobRepository.CountAsync(true, cancellationToken);
        var totalProfiles = await candidateProfileRepository.CountAsync(cancellationToken);
        var providerMetrics = await jobRepository.GetProviderQualityMetricsAsync(cancellationToken);

        return new SystemMetricsDto(
            totalJobs,
            activeJobs,
            totalProfiles,
            await candidateProfileRepository.CountAlertsAsync(cancellationToken),
            await candidateProfileRepository.CountSavedSearchesAsync(cancellationToken),
            await matchExecutionRepository.CountLastDaysAsync(7, cancellationToken),
            providerMetrics);
    }

    public Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProvidersAsync(CancellationToken cancellationToken)
    {
        return jobRepository.GetProviderQualityMetricsAsync(cancellationToken);
    }

    public async Task<MatchingMetricsDto> GetMatchingAsync(CancellationToken cancellationToken)
    {
        var recentExecutions = await matchExecutionRepository.GetLastDaysAsync(7, cancellationToken);
        return new MatchingMetricsDto(
            recentExecutions.FirstOrDefault()?.RuleVersion ?? "unknown",
            recentExecutions.Count,
            recentExecutions.Count == 0 ? null : decimal.Round(recentExecutions.Average(x => x.TopScore ?? 0m), 2),
            recentExecutions.Count == 0 ? null : decimal.Round(recentExecutions.Average(x => x.AverageScore ?? 0m), 2),
            recentExecutions.Sum(x => x.HighMatchCount),
            recentExecutions.Sum(x => x.MediumMatchCount),
            recentExecutions.Sum(x => x.LowMatchCount),
            recentExecutions.Sum(x => x.StrongMatchCount),
            recentExecutions.Sum(x => x.PartialMatchCount),
            recentExecutions.Sum(x => x.HardFailureCount),
            recentExecutions.Sum(x => x.NewHighPriorityCount),
            recentExecutions.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()?.CreatedAtUtc);
    }

    public async Task<AlertMetricsDto> GetAlertsAsync(CancellationToken cancellationToken)
    {
        var recentDeliveries = await alertDeliveryRepository.GetRecentAsync(20, cancellationToken);
        var deliveriesLast7Days = await alertDeliveryRepository.GetLastDaysAsync(7, cancellationToken);

        return new AlertMetricsDto(
            await profileAlertRepository.CountAsync(false, cancellationToken),
            await profileAlertRepository.CountAsync(true, cancellationToken),
            await profileAlertRepository.CountByChannelAsync(AlertChannelType.Webhook, true, cancellationToken),
            await profileAlertRepository.CountByChannelAsync(AlertChannelType.Passive, true, cancellationToken),
            await alertDeliveryRepository.CountLastDaysAsync(7, cancellationToken),
            await alertDeliveryRepository.CountLastDaysByStatusAsync(7, AlertDeliveryStatus.Delivered, cancellationToken),
            await alertDeliveryRepository.CountLastDaysByStatusAsync(7, AlertDeliveryStatus.Failed, cancellationToken),
            await alertDeliveryRepository.CountLastDaysByStatusAsync(7, AlertDeliveryStatus.Recorded, cancellationToken),
            recentDeliveries.Count(x => x.ChannelType == AlertChannelType.Webhook),
            await alertDeliveryRepository.GetLastDeliveredAtUtcAsync(cancellationToken),
            recentDeliveries.Select(RecentAlertDeliveryDto.FromDomain).ToArray());
    }

    public async Task<ProviderOperationsMetricsDto> GetProviderOperationsAsync(CancellationToken cancellationToken)
    {
        var sources = await jobSourceRepository.ListAsync(cancellationToken);
        var recentExecutions = await scrapeExecutionRepository.GetLastDaysAsync(7, cancellationToken);

        var sourceMetrics = sources
            .Select(source =>
            {
                var sourceExecutions = recentExecutions
                    .Where(x => string.Equals(x.SourceName, source.Name, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var successfulRuns = sourceExecutions.Count(x => x.Status == ScrapeExecutionStatus.Completed);
                var failedRuns = sourceExecutions.Count(x => x.Status == ScrapeExecutionStatus.Failed);
                var completedDurations = sourceExecutions
                    .Where(x => x.CompletedAtUtc.HasValue)
                    .Select(x => (decimal)(x.CompletedAtUtc!.Value - x.StartedAtUtc).TotalSeconds)
                    .ToArray();
                var latestExecution = sourceExecutions
                    .OrderByDescending(x => x.StartedAtUtc)
                    .FirstOrDefault();

                return new ProviderOperationMetricsDto(
                    source.Name,
                    source.IsEnabled,
                    source.LastCollectedAtUtc,
                    sourceExecutions.Length,
                    successfulRuns,
                    failedRuns,
                    completedDurations.Length == 0 ? 0m : decimal.Round(completedDurations.Average(), 2),
                    latestExecution?.Status.ToString(),
                    latestExecution?.StartedAtUtc,
                    latestExecution?.CompletedAtUtc,
                    latestExecution?.TotalCollected ?? 0,
                    latestExecution?.CreatedJobs ?? 0,
                    latestExecution?.UpdatedJobs ?? 0,
                    latestExecution?.DeactivatedJobs ?? 0,
                    latestExecution?.ErrorMessage);
            })
            .OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProviderOperationsMetricsDto(sourceMetrics);
    }
}
