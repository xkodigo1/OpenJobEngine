using OpenJobEngine.Application.Jobs;

namespace OpenJobEngine.Application.Common;

public sealed record AlertWebhookPayloadDto(
    string EventType,
    DateTimeOffset SentAtUtc,
    Guid DeliveryId,
    Guid AlertId,
    Guid ProfileId,
    string AlertName,
    string ChannelType,
    decimal MatchScore,
    string MatchBand,
    string RuleVersion,
    IReadOnlyCollection<string> StrongMatches,
    IReadOnlyCollection<string> PartialMatches,
    IReadOnlyCollection<string> HardFailures,
    JobOfferDto Job,
    string? Target);
