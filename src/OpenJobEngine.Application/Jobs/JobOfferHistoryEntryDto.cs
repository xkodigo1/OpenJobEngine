using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Jobs;

public sealed record JobOfferHistoryEntryDto(
    Guid Id,
    string EventType,
    string? SourceName,
    string SnapshotHash,
    string SnapshotJson,
    DateTimeOffset OccurredAtUtc)
{
    public static JobOfferHistoryEntryDto FromDomain(JobOfferHistoryEntry entry)
    {
        return new JobOfferHistoryEntryDto(
            entry.Id,
            entry.EventType.ToString(),
            entry.SourceName,
            entry.SnapshotHash,
            entry.SnapshotJson,
            entry.OccurredAtUtc);
    }
}
