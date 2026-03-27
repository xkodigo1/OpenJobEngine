namespace OpenJobEngine.Infrastructure.Options;

public sealed class PersistenceOptions
{
    public string Provider { get; init; } = "Sqlite";

    public bool ApplyMigrationsOnStartup { get; init; }
}
