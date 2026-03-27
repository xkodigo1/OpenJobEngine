namespace OpenJobEngine.Domain.Entities;

public sealed class MatchExecution
{
    private MatchExecution()
    {
    }

    public MatchExecution(
        Guid id,
        Guid candidateProfileId,
        string? query,
        int resultsCount,
        int totalMatchesCount,
        decimal? topScore,
        decimal? averageScore,
        int highMatchCount,
        int mediumMatchCount,
        int lowMatchCount,
        int strongMatchCount,
        int partialMatchCount,
        int hardFailureCount,
        int newHighPriorityCount,
        decimal? minimumRequestedScore,
        string ruleVersion)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        Query = query;
        ResultsCount = resultsCount;
        TotalMatchesCount = totalMatchesCount;
        TopScore = topScore;
        AverageScore = averageScore;
        HighMatchCount = highMatchCount;
        MediumMatchCount = mediumMatchCount;
        LowMatchCount = lowMatchCount;
        StrongMatchCount = strongMatchCount;
        PartialMatchCount = partialMatchCount;
        HardFailureCount = hardFailureCount;
        NewHighPriorityCount = newHighPriorityCount;
        MinimumRequestedScore = minimumRequestedScore;
        RuleVersion = ruleVersion;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string? Query { get; private set; }

    public int ResultsCount { get; private set; }

    public int TotalMatchesCount { get; private set; }

    public decimal? TopScore { get; private set; }

    public decimal? AverageScore { get; private set; }

    public int HighMatchCount { get; private set; }

    public int MediumMatchCount { get; private set; }

    public int LowMatchCount { get; private set; }

    public int StrongMatchCount { get; private set; }

    public int PartialMatchCount { get; private set; }

    public int HardFailureCount { get; private set; }

    public int NewHighPriorityCount { get; private set; }

    public decimal? MinimumRequestedScore { get; private set; }

    public string RuleVersion { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
