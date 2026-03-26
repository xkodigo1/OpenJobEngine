namespace OpenJobEngine.Application.Profiles;

public sealed record CandidateProfileUpsertRequest(
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
    IReadOnlyCollection<CandidateLanguageInput> Languages);
