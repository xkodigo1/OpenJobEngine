using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IWebhookTestService
{
    Task<WebhookTestResultDto> TestAsync(WebhookTestRequest request, CancellationToken cancellationToken);
}
