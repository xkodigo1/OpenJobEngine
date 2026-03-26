using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Domain.ValueObjects;

namespace OpenJobEngine.Domain.Entities;

public sealed class CandidateProfile
{
    private readonly List<CandidateSkill> skills = [];
    private readonly List<CandidateLanguage> languages = [];
    private readonly List<SavedSearch> savedSearches = [];
    private readonly List<ProfileAlert> alerts = [];

    private CandidateProfile()
    {
    }

    public CandidateProfile(Guid id, string targetTitle, decimal yearsOfExperience, SeniorityLevel seniorityLevel)
    {
        Id = id;
        TargetTitle = targetTitle;
        YearsOfExperience = yearsOfExperience;
        SeniorityLevel = seniorityLevel;
        Preferences = CandidatePreference.Default();
        SalaryExpectation = SalaryExpectation.Empty();
        LocationPreference = LocationPreference.Empty();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string TargetTitle { get; private set; } = string.Empty;

    public string? ProfessionalSummary { get; private set; }

    public decimal YearsOfExperience { get; private set; }

    public SeniorityLevel SeniorityLevel { get; private set; }

    public CandidatePreference Preferences { get; private set; } = CandidatePreference.Default();

    public SalaryExpectation SalaryExpectation { get; private set; } = SalaryExpectation.Empty();

    public LocationPreference LocationPreference { get; private set; } = LocationPreference.Empty();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<CandidateSkill> Skills => skills;

    public IReadOnlyCollection<CandidateLanguage> Languages => languages;

    public IReadOnlyCollection<SavedSearch> SavedSearches => savedSearches;

    public IReadOnlyCollection<ProfileAlert> Alerts => alerts;

    public void UpdateBasics(string targetTitle, string? professionalSummary, decimal yearsOfExperience, SeniorityLevel seniorityLevel)
    {
        TargetTitle = targetTitle.Trim();
        ProfessionalSummary = string.IsNullOrWhiteSpace(professionalSummary) ? null : professionalSummary.Trim();
        YearsOfExperience = yearsOfExperience;
        SeniorityLevel = seniorityLevel;
        Touch();
    }

    public void UpdatePreferences(WorkMode primaryWorkMode, bool acceptRemote, bool acceptHybrid, bool acceptOnSite)
    {
        Preferences.Update(primaryWorkMode, acceptRemote, acceptHybrid, acceptOnSite);
        Touch();
    }

    public void UpdateSalaryExpectation(decimal? minAmount, decimal? maxAmount, string? currency)
    {
        SalaryExpectation.Update(minAmount, maxAmount, currency);
        Touch();
    }

    public void UpdateLocationPreference(
        string? currentCity,
        string? currentRegion,
        string? currentCountryCode,
        IEnumerable<string>? targetCities,
        IEnumerable<string>? targetCountries,
        IEnumerable<string>? targetTimezones,
        bool isWillingToRelocate)
    {
        LocationPreference.Update(currentCity, currentRegion, currentCountryCode, targetCities, targetCountries, targetTimezones, isWillingToRelocate);
        Touch();
    }

    public void UpdateOperationalPreferences(
        IEnumerable<string>? excludedWorkModes,
        IEnumerable<string>? includedCompanyKeywords,
        IEnumerable<string>? excludedCompanyKeywords)
    {
        Preferences.UpdateOperationalPreferences(excludedWorkModes, includedCompanyKeywords, excludedCompanyKeywords);
        Touch();
    }

    public void ReplaceSkills(IEnumerable<CandidateSkill> candidateSkills)
    {
        skills.Clear();
        skills.AddRange(candidateSkills.Select(x => x.CloneForProfile(Id)));
        Touch();
    }

    public void ReplaceLanguages(IEnumerable<CandidateLanguage> candidateLanguages)
    {
        languages.Clear();
        languages.AddRange(candidateLanguages.Select(x => x.CloneForProfile(Id)));
        Touch();
    }

    public void ReplaceSavedSearches(IEnumerable<SavedSearch> searches)
    {
        savedSearches.Clear();
        savedSearches.AddRange(searches.Select(x => x.CloneForProfile(Id)));
        Touch();
    }

    public void AddSavedSearch(SavedSearch search)
    {
        savedSearches.Add(search);
        Touch();
    }

    public void AddAlert(ProfileAlert alert)
    {
        alerts.Add(alert);
        Touch();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
