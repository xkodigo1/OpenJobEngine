using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Exposes the aggregated job catalog, job history and profile-specific match details.
/// </summary>
[ApiController]
[Route("api/jobs")]
public sealed class JobsController(IJobQueryService jobQueryService, IMatchingService matchingService) : ControllerBase
{
    /// <summary>
    /// Searches active jobs using catalog filters such as query, location, salary and source.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<JobOfferDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JobOfferDto>>> GetJobs(
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] bool? remote,
        [FromQuery] decimal? salaryMin,
        [FromQuery] decimal? salaryMax,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var filter = new JobSearchFilter
        {
            Query = query,
            Location = location,
            Remote = remote,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Source = source,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await jobQueryService.SearchAsync(filter, cancellationToken));
    }

    /// <summary>
    /// Alias endpoint for job search.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<JobOfferDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResult<JobOfferDto>>> SearchJobs(
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] bool? remote,
        [FromQuery] decimal? salaryMin,
        [FromQuery] decimal? salaryMax,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return GetJobs(query, location, remote, salaryMin, salaryMax, source, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Returns a single job by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOfferDto>> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobQueryService.GetByIdAsync(id, cancellationToken);
        return Ok(job ?? throw new ResourceNotFoundException($"Job offer '{id:D}' was not found."));
    }

    /// <summary>
    /// Returns the recorded history entries for a single job.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<JobOfferHistoryEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<JobOfferHistoryEntryDto>>> GetJobHistory(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await jobQueryService.GetHistoryAsync(id, cancellationToken));
    }

    /// <summary>
    /// Returns the explainable match result between a profile and a specific job.
    /// </summary>
    [HttpGet("{id:guid}/match")]
    [ProducesResponseType(typeof(JobMatchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobMatchResultDto>> GetJobMatch(
        Guid id,
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var match = await matchingService.GetJobMatchAsync(profileId, id, cancellationToken);
        return Ok(match ?? throw new ResourceNotFoundException($"Match result for profile '{profileId:D}' and job '{id:D}' was not found."));
    }
}
