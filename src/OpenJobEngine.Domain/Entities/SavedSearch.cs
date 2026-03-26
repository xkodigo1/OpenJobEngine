namespace OpenJobEngine.Domain.Entities;

public sealed class SavedSearch
{
    private SavedSearch()
    {
    }

    public SavedSearch(
        Guid id,
        Guid candidateProfileId,
        string name,
        string? query,
        string? location,
        bool? remoteOnly,
        decimal? minimumSalary,
        decimal? minimumMatchScore,
        decimal? minimumNewMatchScore,
        bool onlyNewJobs,
        string? source,
        bool isActive)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        Name = name;
        Query = query;
        Location = location;
        RemoteOnly = remoteOnly;
        MinimumSalary = minimumSalary;
        MinimumMatchScore = minimumMatchScore;
        MinimumNewMatchScore = minimumNewMatchScore;
        OnlyNewJobs = onlyNewJobs;
        Source = source;
        IsActive = isActive;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Query { get; private set; }

    public string? Location { get; private set; }

    public bool? RemoteOnly { get; private set; }

    public decimal? MinimumSalary { get; private set; }

    public decimal? MinimumMatchScore { get; private set; }

    public decimal? MinimumNewMatchScore { get; private set; }

    public bool OnlyNewJobs { get; private set; }

    public string? Source { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public SavedSearch CloneForProfile(Guid candidateProfileId)
    {
        return new SavedSearch(Guid.NewGuid(), candidateProfileId, Name, Query, Location, RemoteOnly, MinimumSalary, MinimumMatchScore, MinimumNewMatchScore, OnlyNewJobs, Source, IsActive);
    }
}
