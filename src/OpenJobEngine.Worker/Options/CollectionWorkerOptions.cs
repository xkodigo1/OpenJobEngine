namespace OpenJobEngine.Worker.Options;

public sealed class CollectionWorkerOptions
{
    public bool Enabled { get; init; }

    public int IntervalMinutes { get; init; } = 60;

    public bool RunOnStartup { get; init; }

    public string? SourceName { get; init; }
}
