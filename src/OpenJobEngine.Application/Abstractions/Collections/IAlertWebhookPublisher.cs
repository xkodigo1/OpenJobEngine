using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IAlertWebhookPublisher
{
    Task<AlertWebhookDispatchResultDto> SendAsync(AlertWebhookPayloadDto payload, CancellationToken cancellationToken);
}
