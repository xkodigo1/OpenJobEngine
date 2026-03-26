using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class ScrapeExecution
{
    private ScrapeExecution()
    {
    }

    private ScrapeExecution(Guid id, string sourceName, DateTimeOffset startedAtUtc)
    {
        Id = id;
        SourceName = sourceName;
        StartedAtUtc = startedAtUtc;
        Status = ScrapeExecutionStatus.Running;
    }

    public Guid Id { get; private set; }

    public string SourceName { get; private set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public ScrapeExecutionStatus Status { get; private set; }

    public int TotalCollected { get; private set; }

    public int CreatedJobs { get; private set; }

    public int UpdatedJobs { get; private set; }

    public int DeduplicatedJobs { get; private set; }

    public int DeactivatedJobs { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static ScrapeExecution Start(string sourceName, DateTimeOffset startedAtUtc)
    {
        return new ScrapeExecution(Guid.NewGuid(), sourceName, startedAtUtc);
    }

    public void Complete(
        DateTimeOffset completedAtUtc,
        int totalCollected,
        int createdJobs,
        int updatedJobs,
        int deduplicatedJobs,
        int deactivatedJobs)
    {
        CompletedAtUtc = completedAtUtc;
        TotalCollected = totalCollected;
        CreatedJobs = createdJobs;
        UpdatedJobs = updatedJobs;
        DeduplicatedJobs = deduplicatedJobs;
        DeactivatedJobs = deactivatedJobs;
        Status = ScrapeExecutionStatus.Completed;
        ErrorMessage = null;
    }

    public void Fail(DateTimeOffset completedAtUtc, string errorMessage)
    {
        CompletedAtUtc = completedAtUtc;
        Status = ScrapeExecutionStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
