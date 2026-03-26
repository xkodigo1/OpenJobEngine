namespace OpenJobEngine.Infrastructure.Options;

public sealed class AdzunaProviderOptions
{
    public bool Enabled { get; init; }

    public string SourceName { get; init; } = "adzuna";

    public string BaseUrl { get; init; } = "https://api.adzuna.com/v1/api/jobs";

    public string CountryCode { get; init; } = "co";

    public string SearchTerm { get; init; } = "developer";

    public string Location { get; init; } = "bogota";

    public int Page { get; init; } = 1;

    public int ResultsPerPage { get; init; } = 20;

    public string AppId { get; init; } = string.Empty;

    public string AppKey { get; init; } = string.Empty;
}
