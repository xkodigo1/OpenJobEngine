namespace OpenJobEngine.Application.Profiles;

public sealed record SavedSearchCreateRequest(
    string Name,
    string? Query,
    string? Location,
    bool? RemoteOnly,
    decimal? MinimumSalary,
    decimal? MinimumMatchScore,
    string? Source,
    bool IsActive);
