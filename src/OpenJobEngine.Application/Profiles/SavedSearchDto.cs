using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Profiles;

public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    string? Query,
    string? Location,
    bool? RemoteOnly,
    decimal? MinimumSalary,
    decimal? MinimumMatchScore,
    decimal? MinimumNewMatchScore,
    bool OnlyNewJobs,
    string? Source,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static SavedSearchDto FromDomain(SavedSearch search) =>
        new(
            search.Id,
            search.Name,
            search.Query,
            search.Location,
            search.RemoteOnly,
            search.MinimumSalary,
            search.MinimumMatchScore,
            search.MinimumNewMatchScore,
            search.OnlyNewJobs,
            search.Source,
            search.IsActive,
            search.CreatedAtUtc);
}
