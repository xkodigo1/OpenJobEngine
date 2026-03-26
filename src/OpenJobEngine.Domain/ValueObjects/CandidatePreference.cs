using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.ValueObjects;

public sealed class CandidatePreference
{
    public WorkMode PreferredPrimaryWorkMode { get; private set; } = WorkMode.Unknown;

    public bool AcceptRemote { get; private set; } = true;

    public bool AcceptHybrid { get; private set; } = true;

    public bool AcceptOnSite { get; private set; } = true;

    public string? ExcludedWorkModesCsv { get; private set; }

    public string? IncludedCompanyKeywordsCsv { get; private set; }

    public string? ExcludedCompanyKeywordsCsv { get; private set; }

    public static CandidatePreference Default() => new();

    public void Update(WorkMode preferredPrimaryWorkMode, bool acceptRemote, bool acceptHybrid, bool acceptOnSite)
    {
        PreferredPrimaryWorkMode = preferredPrimaryWorkMode;
        AcceptRemote = acceptRemote;
        AcceptHybrid = acceptHybrid;
        AcceptOnSite = acceptOnSite;
    }

    public void UpdateOperationalPreferences(
        IEnumerable<string>? excludedWorkModes,
        IEnumerable<string>? includedCompanyKeywords,
        IEnumerable<string>? excludedCompanyKeywords)
    {
        ExcludedWorkModesCsv = JoinNormalizedValues(excludedWorkModes, normalizeUppercase: false);
        IncludedCompanyKeywordsCsv = JoinNormalizedValues(includedCompanyKeywords, normalizeUppercase: false);
        ExcludedCompanyKeywordsCsv = JoinNormalizedValues(excludedCompanyKeywords, normalizeUppercase: false);
    }

    public IReadOnlyCollection<string> GetExcludedWorkModes() => SplitCsv(ExcludedWorkModesCsv);

    public IReadOnlyCollection<string> GetIncludedCompanyKeywords() => SplitCsv(IncludedCompanyKeywordsCsv);

    public IReadOnlyCollection<string> GetExcludedCompanyKeywords() => SplitCsv(ExcludedCompanyKeywordsCsv);

    private static string? JoinNormalizedValues(IEnumerable<string>? values, bool normalizeUppercase)
    {
        var normalized = values?
            .Select(x => string.IsNullOrWhiteSpace(x) ? null : normalizeUppercase ? x.Trim().ToUpperInvariant() : x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 } ? string.Join(",", normalized) : null;
    }

    private static IReadOnlyCollection<string> SplitCsv(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
