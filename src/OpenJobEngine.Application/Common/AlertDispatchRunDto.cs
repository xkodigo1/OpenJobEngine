namespace OpenJobEngine.Application.Common;

public sealed record AlertDispatchRunDto(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int EvaluatedAlerts,
    int MatchedJobs,
    int DeliveredCount,
    int RecordedCount,
    int FailedCount,
    int SkippedCount);
