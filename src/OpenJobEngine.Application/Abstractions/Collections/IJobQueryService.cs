using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IJobQueryService
{
    Task<PagedResult<JobOfferDto>> SearchAsync(JobSearchFilter filter, CancellationToken cancellationToken);

    Task<JobOfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
