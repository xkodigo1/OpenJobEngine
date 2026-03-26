using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/matching")]
public sealed class MatchingController(IMatchingService matchingService) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType(typeof(MatchingSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchingSearchResultDto>> Search(
        [FromBody] MatchingSearchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await matchingService.SearchAsync(request, cancellationToken));
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(MatchingRuleSetDto), StatusCodes.Status200OK)]
    public ActionResult<MatchingRuleSetDto> GetRules()
    {
        return Ok(matchingService.GetCurrentRules());
    }
}
