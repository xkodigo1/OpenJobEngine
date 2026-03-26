using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Profiles;

public sealed record CandidateProfileDto(
    Guid Id,
    string TargetTitle,
    string? ProfessionalSummary,
    decimal YearsOfExperience,
    string SeniorityLevel,
    string PreferredWorkMode,
    bool AcceptRemote,
    bool AcceptHybrid,
    bool AcceptOnSite,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? CurrentCity,
    string? CurrentRegion,
    string? CurrentCountryCode,
    IReadOnlyCollection<string> TargetCities,
    IReadOnlyCollection<string> TargetCountries,
    bool IsWillingToRelocate,
    IReadOnlyCollection<CandidateSkillInput> Skills,
    IReadOnlyCollection<CandidateLanguageInput> Languages,
    IReadOnlyCollection<SavedSearchDto> SavedSearches,
    IReadOnlyCollection<ProfileAlertDto> Alerts,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static CandidateProfileDto FromDomain(CandidateProfile profile)
    {
        return new CandidateProfileDto(
            profile.Id,
            profile.TargetTitle,
            profile.ProfessionalSummary,
            profile.YearsOfExperience,
            profile.SeniorityLevel.ToString(),
            profile.Preferences.PreferredPrimaryWorkMode.ToString(),
            profile.Preferences.AcceptRemote,
            profile.Preferences.AcceptHybrid,
            profile.Preferences.AcceptOnSite,
            profile.SalaryExpectation.MinAmount,
            profile.SalaryExpectation.MaxAmount,
            profile.SalaryExpectation.Currency,
            profile.LocationPreference.CurrentCity,
            profile.LocationPreference.CurrentRegion,
            profile.LocationPreference.CurrentCountryCode,
            profile.LocationPreference.GetTargetCities(),
            profile.LocationPreference.GetTargetCountries(),
            profile.LocationPreference.IsWillingToRelocate,
            profile.Skills.Select(x => new CandidateSkillInput(
                x.SkillName,
                x.SkillSlug,
                x.SkillCategory.ToString(),
                x.YearsExperience,
                x.ProficiencyScore)).ToArray(),
            profile.Languages.Select(x => new CandidateLanguageInput(
                x.LanguageCode,
                x.LanguageName,
                x.Proficiency.ToString())).ToArray(),
            profile.SavedSearches.Select(SavedSearchDto.FromDomain).ToArray(),
            profile.Alerts.Select(ProfileAlertDto.FromDomain).ToArray(),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }
}
