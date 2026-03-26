using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Api.Contracts;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(IWebhookTestService webhookTestService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(WebhookTestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WebhookTestResultDto>> TestWebhook(
        [FromBody] WebhookTestApiRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await webhookTestService.TestAsync(new WebhookTestRequest(request.Url, request.Secret), cancellationToken));
    }
}
