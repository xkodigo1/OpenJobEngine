namespace OpenJobEngine.Domain.Entities;

public sealed class JobSource
{
    private JobSource()
    {
    }

    public JobSource(Guid id, string name, string type, bool isEnabled, string? description)
    {
        Id = id;
        Name = name;
        Type = type;
        IsEnabled = isEnabled;
        Description = description;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset? LastCollectedAtUtc { get; private set; }

    public void UpdateStatus(bool isEnabled, string? description)
    {
        IsEnabled = isEnabled;
        Description = description;
    }

    public void MarkCollected(DateTimeOffset collectedAtUtc)
    {
        LastCollectedAtUtc = collectedAtUtc;
    }
}
