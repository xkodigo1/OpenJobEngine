namespace OpenJobEngine.Infrastructure.Options;

public sealed class LeverProviderOptions
{
    public bool Enabled { get; init; }

    public string SourceName { get; init; } = "lever";

    public string BaseUrl { get; init; } = "https://api.lever.co/v0/postings";

    public string Site { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public int PageSize { get; init; } = 100;

    public int MaxPages { get; init; } = 10;
}
