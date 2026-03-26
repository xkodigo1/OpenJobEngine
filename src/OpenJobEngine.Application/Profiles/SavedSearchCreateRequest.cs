namespace OpenJobEngine.Application.Profiles;

public sealed record SavedSearchCreateRequest(
    string Name,
    string? Query,
    string? Location,
    bool? RemoteOnly,
    decimal? MinimumSalary,
    decimal? MinimumMatchScore,
    decimal? MinimumNewMatchScore,
    bool OnlyNewJobs,
    string? Source,
    bool IsActive);
