namespace OpenJobEngine.Worker.Options;

public sealed class CollectionWorkerOptions
{
    public bool Enabled { get; init; }

    public int IntervalMinutes { get; init; } = 60;

    public bool RunOnStartup { get; init; }

    public string? SourceName { get; init; }

    public int MaxConcurrentSources { get; init; } = 1;

    public int RetryCount { get; init; } = 2;

    public int RetryInitialDelaySeconds { get; init; } = 10;

    public double RetryBackoffMultiplier { get; init; } = 2.0;

    public bool SkipIfAlreadyRunning { get; init; } = true;

    public bool DispatchAlertsAfterCollection { get; init; } = true;
}
