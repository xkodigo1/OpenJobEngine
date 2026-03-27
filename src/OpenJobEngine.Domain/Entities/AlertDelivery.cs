using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class AlertDelivery
{
    private AlertDelivery()
    {
    }

    private AlertDelivery(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }

    public Guid ProfileAlertId { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public Guid JobOfferId { get; private set; }

    public AlertChannelType ChannelType { get; private set; }

    public AlertDeliveryStatus Status { get; private set; }

    public decimal MatchScore { get; private set; }

    public string MatchBand { get; private set; } = string.Empty;

    public string RuleVersion { get; private set; } = string.Empty;

    public int AttemptCount { get; private set; }

    public int? ResponseStatusCode { get; private set; }

    public string? Target { get; private set; }

    public string? LastResponseBody { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset DispatchedAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public static AlertDelivery Create(
        ProfileAlert alert,
        Guid jobOfferId,
        decimal matchScore,
        string matchBand,
        string ruleVersion)
    {
        return new AlertDelivery(Guid.NewGuid())
        {
            ProfileAlertId = alert.Id,
            CandidateProfileId = alert.CandidateProfileId,
            JobOfferId = jobOfferId,
            ChannelType = alert.ChannelType,
            Status = AlertDeliveryStatus.Pending,
            MatchScore = matchScore,
            MatchBand = matchBand,
            RuleVersion = string.IsNullOrWhiteSpace(ruleVersion) ? "unknown" : ruleVersion.Trim(),
            Target = string.IsNullOrWhiteSpace(alert.Target) ? null : alert.Target.Trim(),
            DispatchedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void RecordPassive(DateTimeOffset recordedAtUtc)
    {
        AttemptCount++;
        Status = AlertDeliveryStatus.Recorded;
        ResponseStatusCode = null;
        LastResponseBody = null;
        ErrorMessage = null;
        DeliveredAtUtc = recordedAtUtc;
    }

    public void MarkDelivered(DateTimeOffset deliveredAtUtc, int? statusCode, string? responseBody)
    {
        AttemptCount++;
        Status = AlertDeliveryStatus.Delivered;
        ResponseStatusCode = statusCode;
        LastResponseBody = string.IsNullOrWhiteSpace(responseBody) ? null : responseBody.Trim();
        ErrorMessage = null;
        DeliveredAtUtc = deliveredAtUtc;
    }

    public void MarkFailed(DateTimeOffset attemptedAtUtc, int? statusCode, string? responseBody, string? errorMessage)
    {
        AttemptCount++;
        Status = AlertDeliveryStatus.Failed;
        ResponseStatusCode = statusCode;
        LastResponseBody = string.IsNullOrWhiteSpace(responseBody) ? null : responseBody.Trim();
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown alert delivery error." : errorMessage.Trim();
        DeliveredAtUtc = attemptedAtUtc;
    }

    public bool IsTerminal()
    {
        return Status is AlertDeliveryStatus.Delivered or AlertDeliveryStatus.Recorded;
    }
}
