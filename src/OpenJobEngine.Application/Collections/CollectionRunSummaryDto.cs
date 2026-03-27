namespace OpenJobEngine.Application.Collections;

public sealed record CollectionRunSummaryDto(
    string SourceName,
    int TotalCollected,
    int CreatedJobs,
    int UpdatedJobs,
    int DeduplicatedJobs,
    int DeactivatedJobs,
    int StaleDeactivatedJobs,
    bool Success,
    string? ErrorMessage);
