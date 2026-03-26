namespace OpenJobEngine.Application.Profiles;

public sealed record ProfileAlertCreateRequest(
    string Name,
    string ChannelType,
    string? Target,
    decimal? MinimumMatchScore,
    bool IsActive);
