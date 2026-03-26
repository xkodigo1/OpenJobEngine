namespace OpenJobEngine.Domain.Entities;

public sealed class JobOfferSourceObservation
{
    private JobOfferSourceObservation()
    {
    }

    public JobOfferSourceObservation(
        Guid id,
        Guid jobOfferId,
        string sourceName,
        string sourceJobId,
        bool isActive,
        DateTimeOffset firstSeenAtUtc,
        DateTimeOffset lastSeenAtUtc,
        string snapshotHash)
    {
        Id = id;
        JobOfferId = jobOfferId;
        SourceName = sourceName;
        SourceJobId = sourceJobId;
        IsActive = isActive;
        FirstSeenAtUtc = firstSeenAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
        SnapshotHash = snapshotHash;
    }

    public Guid Id { get; private set; }

    public Guid JobOfferId { get; private set; }

    public string SourceName { get; private set; } = string.Empty;

    public string SourceJobId { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset FirstSeenAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public string SnapshotHash { get; private set; } = string.Empty;

    public void ReassignJobOffer(Guid jobOfferId)
    {
        JobOfferId = jobOfferId;
    }

    public void MarkSeen(DateTimeOffset seenAtUtc, string snapshotHash)
    {
        IsActive = true;
        if (seenAtUtc > LastSeenAtUtc)
        {
            LastSeenAtUtc = seenAtUtc;
        }

        if (string.IsNullOrWhiteSpace(SnapshotHash) || !string.Equals(SnapshotHash, snapshotHash, StringComparison.Ordinal))
        {
            SnapshotHash = snapshotHash;
        }
    }

    public void MarkInactive()
    {
        IsActive = false;
    }

    public JobOfferSourceObservation CloneForJob(Guid jobOfferId)
    {
        return new JobOfferSourceObservation(Id, jobOfferId, SourceName, SourceJobId, IsActive, FirstSeenAtUtc, LastSeenAtUtc, SnapshotHash);
    }
}
