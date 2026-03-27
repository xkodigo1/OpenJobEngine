using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes operational alert-dispatch actions for manual validation and integrations.
/// </summary>
[ApiController]
[Route("api/alerts")]
public sealed class AlertsController(IAlertDispatchService alertDispatchService) : ControllerBase
{
    /// <summary>
    /// Dispatches all currently active alerts and returns the execution summary.
    /// </summary>
    [HttpPost("dispatch")]
    [ProducesResponseType(typeof(AlertDispatchRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertDispatchRunDto>> Dispatch(CancellationToken cancellationToken)
    {
        return Ok(await alertDispatchService.DispatchActiveAlertsAsync(cancellationToken));
    }
}
