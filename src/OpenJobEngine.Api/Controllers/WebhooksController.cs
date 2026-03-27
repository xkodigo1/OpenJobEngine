using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Api.Contracts;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes webhook utility endpoints for manual integration checks.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(IWebhookTestService webhookTestService) : ControllerBase
{
    /// <summary>
    /// Sends a signed test payload to a target webhook URL.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(WebhookTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WebhookTestResultDto>> TestWebhook(
        [FromBody] WebhookTestApiRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await webhookTestService.TestAsync(new WebhookTestRequest(request.Url, request.Secret), cancellationToken));
    }
}
