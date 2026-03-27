using System.Net.Http.Json;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Infrastructure.Services;

public sealed class AlertWebhookPublisher(IHttpClientFactory httpClientFactory) : IAlertWebhookPublisher
{
    public async Task<AlertWebhookDispatchResultDto> SendAsync(
        AlertWebhookPayloadDto payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Target))
        {
            return new AlertWebhookDispatchResultDto(
                false,
                null,
                null,
                "Webhook target URL is required for webhook alerts.");
        }

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, payload.Target)
        {
            Content = JsonContent.Create(new
            {
                type = payload.EventType,
                sentAtUtc = payload.SentAtUtc,
                deliveryId = payload.DeliveryId,
                alertId = payload.AlertId,
                profileId = payload.ProfileId,
                alertName = payload.AlertName,
                channelType = payload.ChannelType,
                matchScore = payload.MatchScore,
                matchBand = payload.MatchBand,
                ruleVersion = payload.RuleVersion,
                strongMatches = payload.StrongMatches,
                partialMatches = payload.PartialMatches,
                hardFailures = payload.HardFailures,
                job = payload.Job
            })
        };

        request.Headers.Add("X-OpenJobEngine-Event", payload.EventType);
        request.Headers.Add("X-OpenJobEngine-AlertId", payload.AlertId.ToString("D"));
        request.Headers.Add("X-OpenJobEngine-ProfileId", payload.ProfileId.ToString("D"));
        request.Headers.Add("X-OpenJobEngine-DeliveryId", payload.DeliveryId.ToString("D"));

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AlertWebhookDispatchResultDto(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                string.IsNullOrWhiteSpace(responseBody) ? null : responseBody,
                response.IsSuccessStatusCode ? null : "Webhook returned a non-success status code.");
        }
        catch (Exception exception)
        {
            return new AlertWebhookDispatchResultDto(false, null, null, exception.Message);
        }
    }
}
