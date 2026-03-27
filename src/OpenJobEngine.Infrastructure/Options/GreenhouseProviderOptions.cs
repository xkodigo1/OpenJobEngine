namespace OpenJobEngine.Infrastructure.Options;

public sealed class GreenhouseProviderOptions
{
    public bool Enabled { get; init; }

    public string SourceName { get; init; } = "greenhouse";

    public string BaseUrl { get; init; } = "https://boards-api.greenhouse.io/v1/boards";

    public string BoardToken { get; init; } = string.Empty;

    public bool IncludeContent { get; init; } = true;

    public int StaleAfterHours { get; init; } = 168;
}
