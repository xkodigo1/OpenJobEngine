using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Collections;

public sealed record ScrapeExecutionDto(
    Guid Id,
    string SourceName,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int TotalCollected,
    int CreatedJobs,
    int UpdatedJobs,
    int DeduplicatedJobs,
    string? ErrorMessage)
{
    public static ScrapeExecutionDto FromDomain(ScrapeExecution execution)
    {
        return new ScrapeExecutionDto(
            execution.Id,
            execution.SourceName,
            execution.Status.ToString(),
            execution.StartedAtUtc,
            execution.CompletedAtUtc,
            execution.TotalCollected,
            execution.CreatedJobs,
            execution.UpdatedJobs,
            execution.DeduplicatedJobs,
            execution.ErrorMessage);
    }
}
