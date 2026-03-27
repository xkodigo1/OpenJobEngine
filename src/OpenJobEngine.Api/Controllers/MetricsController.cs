using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes operational metrics for the job catalog and the enabled providers.
/// </summary>
[ApiController]
[Route("api/metrics")]
public sealed class MetricsController(ISystemMetricsService systemMetricsService) : ControllerBase
{
    /// <summary>
    /// Returns platform-level overview metrics.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(SystemMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemMetricsDto>> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await systemMetricsService.GetOverviewAsync(cancellationToken));
    }

    /// <summary>
    /// Returns provider quality metrics derived from active source observations.
    /// </summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProviderQualityMetricsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProviderQualityMetricsDto>>> GetProviders(CancellationToken cancellationToken)
    {
        return Ok(await systemMetricsService.GetProvidersAsync(cancellationToken));
    }
}
