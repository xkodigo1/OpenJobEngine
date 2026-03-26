namespace OpenJobEngine.Application.Profiles;

public sealed record CandidatePreferencesUpdateRequest(
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
    bool IsWillingToRelocate);
