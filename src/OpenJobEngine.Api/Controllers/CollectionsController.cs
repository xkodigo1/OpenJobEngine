using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Collections;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/collections")]
public sealed class CollectionsController(IJobCollectionService jobCollectionService) : ControllerBase
{
    [HttpPost("run")]
    [ProducesResponseType(typeof(CollectionRunResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionRunResultDto>> RunAllCollections(CancellationToken cancellationToken)
    {
        return Ok(await jobCollectionService.RunAllAsync(cancellationToken));
    }

    [HttpPost("run/{source}")]
    [ProducesResponseType(typeof(CollectionRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionRunResultDto>> RunCollectionBySource(string source, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await jobCollectionService.RunSourceAsync(source, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("executions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ScrapeExecutionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ScrapeExecutionDto>>> GetExecutions(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await jobCollectionService.GetRecentExecutionsAsync(take, cancellationToken));
    }
}
