using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController(ISystemMetricsService systemMetricsService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(SystemMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemMetricsDto>> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await systemMetricsService.GetOverviewAsync(cancellationToken));
    }

    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProviderQualityMetricsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProviderQualityMetricsDto>>> GetProviders(CancellationToken cancellationToken)
    {
        return Ok(await systemMetricsService.GetProvidersAsync(cancellationToken));
    }
}
