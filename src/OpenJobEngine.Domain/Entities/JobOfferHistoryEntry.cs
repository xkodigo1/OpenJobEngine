using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class JobOfferHistoryEntry
{
    private JobOfferHistoryEntry()
    {
    }

    public JobOfferHistoryEntry(
        Guid id,
        Guid jobOfferId,
        JobOfferHistoryEventType eventType,
        string snapshotHash,
        string snapshotJson,
        string? sourceName,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        JobOfferId = jobOfferId;
        EventType = eventType;
        SnapshotHash = snapshotHash;
        SnapshotJson = snapshotJson;
        SourceName = sourceName;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid JobOfferId { get; private set; }

    public JobOfferHistoryEventType EventType { get; private set; }

    public string SnapshotHash { get; private set; } = string.Empty;

    public string SnapshotJson { get; private set; } = string.Empty;

    public string? SourceName { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public JobOfferHistoryEntry CloneForJob(Guid jobOfferId)
    {
        return new JobOfferHistoryEntry(Id, jobOfferId, EventType, SnapshotHash, SnapshotJson, SourceName, OccurredAtUtc);
    }
}
