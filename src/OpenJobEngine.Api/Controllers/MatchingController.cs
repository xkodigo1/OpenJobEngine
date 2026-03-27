using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes explainable match search and matching rules inspection endpoints.
/// </summary>
[ApiController]
[Route("api/matching")]
public sealed class MatchingController(IMatchingService matchingService) : ControllerBase
{
    /// <summary>
    /// Searches jobs ranked for a specific profile using deterministic matching rules.
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(MatchingSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchingSearchResultDto>> Search(
        [FromBody] MatchingSearchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await matchingService.SearchAsync(request, cancellationToken));
    }

    /// <summary>
    /// Returns the currently active deterministic matching rule set.
    /// </summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(MatchingRuleSetDto), StatusCodes.Status200OK)]
    public ActionResult<MatchingRuleSetDto> GetRules()
    {
        return Ok(matchingService.GetCurrentRules());
    }
}
