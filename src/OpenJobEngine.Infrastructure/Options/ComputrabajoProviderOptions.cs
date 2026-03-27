namespace OpenJobEngine.Infrastructure.Options;

public sealed class ComputrabajoProviderOptions
{
    public bool Enabled { get; init; }

    public string SourceName { get; init; } = "computrabajo";

    public string BaseUrl { get; init; } = "https://www.computrabajo.com.co";

    public string SearchTerm { get; init; } = "developer";

    public int MaxPages { get; init; } = 1;

    public int DelayMs { get; init; } = 1000;

    public bool UsePlaywright { get; init; }

    public string UserAgent { get; init; } = "OpenJobEngine/1.0 (+https://github.com)";

    public int StaleAfterHours { get; init; } = 72;
}
