using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class JobOffer
{
    private readonly List<JobOfferSkillTag> skillTags = [];
    private readonly List<JobOfferLanguageRequirement> languageRequirements = [];
    private readonly List<JobOfferSourceObservation> sourceObservations = [];
    private readonly List<JobOfferHistoryEntry> historyEntries = [];

    private JobOffer()
    {
    }

    public JobOffer(
        Guid id,
        string title,
        string companyName,
        string? description,
        string? locationText,
        EmploymentType employmentType,
        SeniorityLevel seniorityLevel,
        WorkMode workMode,
        string? city,
        string? region,
        string? countryCode,
        string? salaryText,
        decimal? salaryMin,
        decimal? salaryMax,
        string? salaryCurrency,
        bool isRemote,
        string url,
        string sourceName,
        string sourceJobId,
        DateTimeOffset? publishedAtUtc,
        DateTimeOffset collectedAtUtc,
        DateTimeOffset lastSeenAtUtc,
        string deduplicationKey,
        bool isActive,
        decimal qualityScore,
        string? qualityFlags)
    {
        Id = id;
        Title = title;
        CompanyName = companyName;
        Description = description;
        LocationText = locationText;
        EmploymentType = employmentType;
        SeniorityLevel = seniorityLevel;
        WorkMode = workMode;
        City = city;
        Region = region;
        CountryCode = countryCode;
        SalaryText = salaryText;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryCurrency = salaryCurrency;
        IsRemote = isRemote;
        Url = url;
        SourceName = sourceName;
        SourceJobId = sourceJobId;
        PublishedAtUtc = publishedAtUtc;
        CollectedAtUtc = collectedAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
        DeduplicationKey = deduplicationKey;
        IsActive = isActive;
        QualityScore = qualityScore;
        QualityFlags = qualityFlags;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? LocationText { get; private set; }

    public EmploymentType EmploymentType { get; private set; }

    public SeniorityLevel SeniorityLevel { get; private set; }

    public WorkMode WorkMode { get; private set; }

    public string? City { get; private set; }

    public string? Region { get; private set; }

    public string? CountryCode { get; private set; }

    public string? SalaryText { get; private set; }

    public decimal? SalaryMin { get; private set; }

    public decimal? SalaryMax { get; private set; }

    public string? SalaryCurrency { get; private set; }

    public bool IsRemote { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public string SourceName { get; private set; } = string.Empty;

    public string SourceJobId { get; private set; } = string.Empty;

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset CollectedAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public string DeduplicationKey { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public decimal QualityScore { get; private set; }

    public string? QualityFlags { get; private set; }

    public IReadOnlyCollection<JobOfferSkillTag> SkillTags => skillTags;

    public IReadOnlyCollection<JobOfferLanguageRequirement> LanguageRequirements => languageRequirements;

    public IReadOnlyCollection<JobOfferSourceObservation> SourceObservations => sourceObservations;

    public IReadOnlyCollection<JobOfferHistoryEntry> HistoryEntries => historyEntries;

    public void RefreshFrom(JobOffer incoming, string deduplicationKey, bool preserveSourceIdentity = false)
    {
        Title = incoming.Title;
        CompanyName = incoming.CompanyName;
        Description = incoming.Description;
        LocationText = incoming.LocationText;
        EmploymentType = incoming.EmploymentType;
        SeniorityLevel = incoming.SeniorityLevel;
        WorkMode = incoming.WorkMode;
        City = incoming.City;
        Region = incoming.Region;
        CountryCode = incoming.CountryCode;
        SalaryText = incoming.SalaryText;
        SalaryMin = incoming.SalaryMin;
        SalaryMax = incoming.SalaryMax;
        SalaryCurrency = incoming.SalaryCurrency;
        IsRemote = incoming.IsRemote;
        if (!preserveSourceIdentity)
        {
            Url = incoming.Url;
            SourceName = incoming.SourceName;
            SourceJobId = incoming.SourceJobId;
        }

        PublishedAtUtc = incoming.PublishedAtUtc;
        CollectedAtUtc = incoming.CollectedAtUtc;
        LastSeenAtUtc = incoming.LastSeenAtUtc;
        DeduplicationKey = deduplicationKey;
        QualityScore = incoming.QualityScore;
        QualityFlags = incoming.QualityFlags;
        ReplaceSkillTags(incoming.SkillTags);
        ReplaceLanguageRequirements(incoming.LanguageRequirements);
        IsActive = true;
    }

    public void AssignDeduplicationKey(string key)
    {
        DeduplicationKey = key;
    }

    public void MarkSeen(DateTimeOffset seenAtUtc)
    {
        if (seenAtUtc > LastSeenAtUtc)
        {
            LastSeenAtUtc = seenAtUtc;
        }

        IsActive = true;
    }

    public void SetLocation(string? city, string? region, string? countryCode, WorkMode workMode)
    {
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant();
        WorkMode = workMode;
        IsRemote = workMode == WorkMode.Remote;
    }

    public void SetSeniorityLevel(SeniorityLevel seniorityLevel)
    {
        SeniorityLevel = seniorityLevel;
    }

    public void SetSalary(string? salaryText, decimal? salaryMin, decimal? salaryMax, string? salaryCurrency)
    {
        SalaryText = string.IsNullOrWhiteSpace(salaryText) ? null : salaryText.Trim();
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryCurrency = string.IsNullOrWhiteSpace(salaryCurrency) ? null : salaryCurrency.Trim().ToUpperInvariant();
    }

    public void SetQuality(decimal qualityScore, IEnumerable<string>? qualityFlags)
    {
        QualityScore = qualityScore;
        var normalizedFlags = qualityFlags?
            .Select(x => string.IsNullOrWhiteSpace(x) ? null : x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        QualityFlags = normalizedFlags is { Length: > 0 } ? string.Join(",", normalizedFlags) : null;
    }

    public IReadOnlyCollection<string> GetQualityFlags()
    {
        return string.IsNullOrWhiteSpace(QualityFlags)
            ? Array.Empty<string>()
            : QualityFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public void ReplaceSkillTags(IEnumerable<JobOfferSkillTag> tags)
    {
        skillTags.Clear();
        skillTags.AddRange(tags.Select(x => x.CloneForJob(Id)));
    }

    public void ReplaceLanguageRequirements(IEnumerable<JobOfferLanguageRequirement> requirements)
    {
        languageRequirements.Clear();
        languageRequirements.AddRange(requirements.Select(x => x.CloneForJob(Id)));
    }

    public void ReplaceSourceObservations(IEnumerable<JobOfferSourceObservation> observations)
    {
        sourceObservations.Clear();
        sourceObservations.AddRange(observations.Select(x => x.CloneForJob(Id)));
    }

    public void AddOrReplaceObservation(JobOfferSourceObservation observation)
    {
        var existing = sourceObservations.FirstOrDefault(x =>
            string.Equals(x.SourceName, observation.SourceName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.SourceJobId, observation.SourceJobId, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            sourceObservations.Remove(existing);
        }

        sourceObservations.Add(observation.CloneForJob(Id));
    }

    public void AddHistoryEntry(JobOfferHistoryEntry entry)
    {
        historyEntries.Add(entry.CloneForJob(Id));
    }

    public void MarkInactive()
    {
        IsActive = false;
    }

    public void SetActiveState(bool isActive)
    {
        IsActive = isActive;
    }
}
