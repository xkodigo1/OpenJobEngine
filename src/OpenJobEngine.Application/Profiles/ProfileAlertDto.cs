using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Profiles;

public sealed record ProfileAlertDto(
    Guid Id,
    string Name,
    string ChannelType,
    string? Target,
    decimal? MinimumMatchScore,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ProfileAlertDto FromDomain(ProfileAlert alert) =>
        new(
            alert.Id,
            alert.Name,
            alert.ChannelType.ToString(),
            alert.Target,
            alert.MinimumMatchScore,
            alert.IsActive,
            alert.CreatedAtUtc);
}
