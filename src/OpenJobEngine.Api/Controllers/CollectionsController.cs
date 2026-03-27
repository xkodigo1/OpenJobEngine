using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Collections;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes collection runs and execution history for the enabled providers.
/// </summary>
[ApiController]
[Route("api/collections")]
public sealed class CollectionsController(IJobCollectionService jobCollectionService) : ControllerBase
{
    /// <summary>
    /// Executes a collection run across all enabled providers.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(CollectionRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionRunResultDto>> RunAllCollections(CancellationToken cancellationToken)
    {
        return Ok(await jobCollectionService.RunAllAsync(cancellationToken));
    }

    /// <summary>
    /// Executes a collection run for a single provider.
    /// </summary>
    [HttpPost("run/{source}")]
    [ProducesResponseType(typeof(CollectionRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionRunResultDto>> RunCollectionBySource(
        [FromRoute] string source,
        CancellationToken cancellationToken)
    {
        return Ok(await jobCollectionService.RunSourceAsync(source, cancellationToken));
    }

    /// <summary>
    /// Returns recent collection executions ordered from newest to oldest.
    /// </summary>
    [HttpGet("executions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ScrapeExecutionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ScrapeExecutionDto>>> GetExecutions(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await jobCollectionService.GetRecentExecutionsAsync(take, cancellationToken));
    }
}
