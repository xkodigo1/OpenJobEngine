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
        decimal? topScore,
        decimal? averageScore,
        int highMatchCount,
        int mediumMatchCount,
        int lowMatchCount,
        string ruleVersion)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        Query = query;
        ResultsCount = resultsCount;
        TopScore = topScore;
        AverageScore = averageScore;
        HighMatchCount = highMatchCount;
        MediumMatchCount = mediumMatchCount;
        LowMatchCount = lowMatchCount;
        RuleVersion = ruleVersion;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string? Query { get; private set; }

    public int ResultsCount { get; private set; }

    public decimal? TopScore { get; private set; }

    public decimal? AverageScore { get; private set; }

    public int HighMatchCount { get; private set; }

    public int MediumMatchCount { get; private set; }

    public int LowMatchCount { get; private set; }

    public string RuleVersion { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
