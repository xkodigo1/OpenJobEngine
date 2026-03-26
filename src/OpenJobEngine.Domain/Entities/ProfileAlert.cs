using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class ProfileAlert
{
    private ProfileAlert()
    {
    }

    public ProfileAlert(
        Guid id,
        Guid candidateProfileId,
        string name,
        AlertChannelType channelType,
        string? target,
        decimal? minimumMatchScore,
        bool isActive)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        Name = name;
        ChannelType = channelType;
        Target = target;
        MinimumMatchScore = minimumMatchScore;
        IsActive = isActive;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public AlertChannelType ChannelType { get; private set; }

    public string? Target { get; private set; }

    public decimal? MinimumMatchScore { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ProfileAlert CloneForProfile(Guid candidateProfileId)
    {
        return new ProfileAlert(Guid.NewGuid(), candidateProfileId, Name, ChannelType, Target, MinimumMatchScore, IsActive);
    }
}
