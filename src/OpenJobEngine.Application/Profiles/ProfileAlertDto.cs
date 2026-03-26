using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Profiles;

public sealed record ProfileAlertDto(
    Guid Id,
    string Name,
    string ChannelType,
    string? Target,
    decimal? MinimumMatchScore,
    decimal? MinimumNewMatchScore,
    bool OnlyNewJobs,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastCheckedAtUtc)
{
    public static ProfileAlertDto FromDomain(ProfileAlert alert) =>
        new(
            alert.Id,
            alert.Name,
            alert.ChannelType.ToString(),
            alert.Target,
            alert.MinimumMatchScore,
            alert.MinimumNewMatchScore,
            alert.OnlyNewJobs,
            alert.IsActive,
            alert.CreatedAtUtc,
            alert.LastCheckedAtUtc);
}
