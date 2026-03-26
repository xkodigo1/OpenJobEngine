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
    decimal JobsWithStructuredLocationRatio,
    decimal JobsWithSkillSignalsRatio,
    decimal JobsWithLanguageSignalsRatio);
