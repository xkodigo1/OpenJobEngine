namespace OpenJobEngine.Application.Common;

public sealed record SystemMetricsDto(
    long TotalJobs,
    long ActiveJobs,
    int TotalProfiles,
    int TotalAlerts,
    int TotalSavedSearches,
    int MatchExecutionsLast7Days,
    IReadOnlyCollection<ProviderQualityMetricsDto> ProviderMetrics);

public sealed record ProviderQualityMetricsDto(
    string SourceName,
    int TotalActiveJobs,
    decimal AverageQualityScore,
    decimal JobsWithSalaryRatio,
    decimal JobsWithTrustedSalaryRatio,
    decimal JobsWithStructuredLocationRatio,
    decimal JobsWithSkillSignalsRatio,
    decimal JobsWithLanguageSignalsRatio,
    decimal LowQualityJobsRatio,
    decimal? AverageFreshnessHours);

public sealed record ProviderOperationsMetricsDto(
    IReadOnlyCollection<ProviderOperationMetricsDto> Sources);

public sealed record ProviderOperationMetricsDto(
    string SourceName,
    bool IsEnabled,
    DateTimeOffset? LastCollectedAtUtc,
    int TotalRunsLast7Days,
    int SuccessfulRunsLast7Days,
    int FailedRunsLast7Days,
    decimal AverageDurationSeconds,
    string? LastStatus,
    DateTimeOffset? LastStartedAtUtc,
    DateTimeOffset? LastCompletedAtUtc,
    int LastTotalCollected,
    int LastCreatedJobs,
    int LastUpdatedJobs,
    int LastDeactivatedJobs,
    string? LastErrorMessage);

public sealed record MatchingMetricsDto(
    string RuleVersion,
    int ExecutionsLast7Days,
    decimal? AverageTopScore,
    decimal? AverageAverageScore,
    int HighMatchCount,
    int MediumMatchCount,
    int LowMatchCount,
    int StrongMatchCount,
    int PartialMatchCount,
    int HardFailureCount,
    int NewHighPriorityCount,
    DateTimeOffset? LastExecutionAtUtc);

public sealed record AlertMetricsDto(
    int TotalAlerts,
    int ActiveAlerts,
    int ActiveWebhookAlerts,
    int ActivePassiveAlerts,
    long DeliveriesLast7Days,
    long SuccessfulDeliveriesLast7Days,
    long FailedDeliveriesLast7Days,
    long RecordedDeliveriesLast7Days,
    int WebhookDeliveriesLast7Days,
    DateTimeOffset? LastDeliveredAtUtc,
    IReadOnlyCollection<RecentAlertDeliveryDto> RecentDeliveries);

public sealed record RecentAlertDeliveryDto(
    Guid DeliveryId,
    Guid AlertId,
    Guid ProfileId,
    Guid JobId,
    string ChannelType,
    string Status,
    decimal MatchScore,
    string MatchBand,
    int AttemptCount,
    int? LastStatusCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string? DeliveryTarget,
    string? LastErrorMessage)
{
    public static RecentAlertDeliveryDto FromDomain(OpenJobEngine.Domain.Entities.AlertDelivery delivery)
    {
        return new RecentAlertDeliveryDto(
            delivery.Id,
            delivery.ProfileAlertId,
            delivery.CandidateProfileId,
            delivery.JobOfferId,
            delivery.ChannelType.ToString(),
            delivery.Status.ToString(),
            delivery.MatchScore,
            delivery.MatchBand,
            delivery.AttemptCount,
            delivery.ResponseStatusCode,
            delivery.DispatchedAtUtc,
            delivery.DeliveredAtUtc,
            delivery.Target,
            delivery.ErrorMessage);
    }
}
