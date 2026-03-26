namespace OpenJobEngine.Infrastructure.Options;

public sealed class PersistenceOptions
{
    public string Provider { get; init; } = "Sqlite";
}
