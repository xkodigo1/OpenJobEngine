namespace OpenJobEngine.Application.Jobs;

public sealed class JobSearchFilter
{
    public string? Query { get; init; }

    public string? Location { get; init; }

    public bool? Remote { get; init; }

    public decimal? SalaryMin { get; init; }

    public decimal? SalaryMax { get; init; }

    public string? Source { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
