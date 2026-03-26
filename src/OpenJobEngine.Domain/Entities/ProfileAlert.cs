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
        decimal? minimumNewMatchScore,
        bool onlyNewJobs,
        bool isActive)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        Name = name;
        ChannelType = channelType;
        Target = target;
        MinimumMatchScore = minimumMatchScore;
        MinimumNewMatchScore = minimumNewMatchScore;
        OnlyNewJobs = onlyNewJobs;
        IsActive = isActive;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastCheckedAtUtc = null;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public AlertChannelType ChannelType { get; private set; }

    public string? Target { get; private set; }

    public decimal? MinimumMatchScore { get; private set; }

    public decimal? MinimumNewMatchScore { get; private set; }

    public bool OnlyNewJobs { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastCheckedAtUtc { get; private set; }

    public void MarkChecked(DateTimeOffset checkedAtUtc)
    {
        LastCheckedAtUtc = checkedAtUtc;
    }

    public ProfileAlert CloneForProfile(Guid candidateProfileId)
    {
        return new ProfileAlert(Guid.NewGuid(), candidateProfileId, Name, ChannelType, Target, MinimumMatchScore, MinimumNewMatchScore, OnlyNewJobs, IsActive);
    }
}
