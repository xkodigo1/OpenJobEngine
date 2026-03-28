using OpenJobEngine.Application.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Worker.Services;

public sealed record WorkerCycleSummary(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    bool WorkerEnabled,
    int SelectedSources,
    int SuccessfulSources,
    int FailedSources,
    int SkippedSources,
    int TimedOutSources,
    int TotalCollected,
    int CreatedJobs,
    int UpdatedJobs,
    int DeduplicatedJobs,
    int DeactivatedJobs,
    bool AlertsDispatched,
    WorkerAlertDispatchSummary? AlertDispatch,
    IReadOnlyCollection<WorkerSourceRunSummary> Sources)
{
    public static WorkerCycleSummary Disabled(DateTimeOffset startedAtUtc)
    {
        return new WorkerCycleSummary(
            startedAtUtc,
            startedAtUtc,
            false,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            null,
            Array.Empty<WorkerSourceRunSummary>());
    }
}

public sealed record WorkerSourceRunSummary(
    string SourceName,
    bool Success,
    bool Skipped,
    bool TimedOut,
    int Attempts,
    int TotalCollected,
    int CreatedJobs,
    int UpdatedJobs,
    int DeduplicatedJobs,
    int DeactivatedJobs,
    string? ErrorMessage)
{
    public static WorkerSourceRunSummary FromCollectionSummary(
        CollectionRunSummaryDto summary,
        int attempts,
        bool timedOut)
    {
        return new WorkerSourceRunSummary(
            summary.SourceName,
            summary.Success,
            false,
            timedOut,
            attempts,
            summary.TotalCollected,
            summary.CreatedJobs,
            summary.UpdatedJobs,
            summary.DeduplicatedJobs,
            summary.DeactivatedJobs,
            summary.ErrorMessage);
    }

    public static WorkerSourceRunSummary Failed(
        string sourceName,
        int attempts,
        bool timedOut,
        string? errorMessage)
    {
        return new WorkerSourceRunSummary(sourceName, false, false, timedOut, attempts, 0, 0, 0, 0, 0, errorMessage);
    }

    public static WorkerSourceRunSummary CreateSkipped(string sourceName)
    {
        return new WorkerSourceRunSummary(sourceName, false, true, false, 0, 0, 0, 0, 0, 0, null);
    }
}

public sealed record WorkerAlertDispatchSummary(
    bool Success,
    int EvaluatedAlerts,
    int MatchedJobs,
    int DeliveredCount,
    int RecordedCount,
    int FailedCount,
    int SkippedCount,
    string? ErrorMessage)
{
    public static WorkerAlertDispatchSummary FromDto(AlertDispatchRunDto dto)
    {
        return new WorkerAlertDispatchSummary(
            dto.FailedCount == 0,
            dto.EvaluatedAlerts,
            dto.MatchedJobs,
            dto.DeliveredCount,
            dto.RecordedCount,
            dto.FailedCount,
            dto.SkippedCount,
            null);
    }

    public static WorkerAlertDispatchSummary Failed(string errorMessage)
    {
        return new WorkerAlertDispatchSummary(false, 0, 0, 0, 0, 0, 0, errorMessage);
    }
}
