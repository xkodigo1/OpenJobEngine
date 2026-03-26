namespace OpenJobEngine.Application.Matching;

public sealed record MatchingSearchRequest(
    Guid ProfileId,
    string? Query,
    string? Location,
    bool? RemoteOnly,
    decimal? SalaryMin,
    string? Source,
    int Page,
    int PageSize,
    decimal? MinimumMatchScore);
