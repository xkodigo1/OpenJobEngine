using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(IJobQueryService jobQueryService) : ControllerBase
{
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOfferDto>> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobQueryService.GetByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }
}
