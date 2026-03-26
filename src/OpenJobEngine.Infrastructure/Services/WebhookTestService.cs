using System.Net.Http.Json;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Infrastructure.Services;

public sealed class WebhookTestService(IHttpClientFactory httpClientFactory) : IWebhookTestService
{
    public async Task<WebhookTestResultDto> TestAsync(WebhookTestRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, request.Url)
        {
            Content = JsonContent.Create(new
            {
                type = "openjobengine.webhook.test",
                sentAtUtc = DateTimeOffset.UtcNow
            })
        };

        if (!string.IsNullOrWhiteSpace(request.Secret))
        {
            message.Headers.Add("X-OpenJobEngine-Secret", request.Secret);
        }

        try
        {
            var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WebhookTestResultDto(response.IsSuccessStatusCode, (int)response.StatusCode, body);
        }
        catch (Exception exception)
        {
            return new WebhookTestResultDto(false, 0, exception.Message);
        }
    }
}
