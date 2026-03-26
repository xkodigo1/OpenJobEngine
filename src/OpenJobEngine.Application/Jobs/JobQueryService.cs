using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Jobs;

public sealed class JobQueryService(IJobRepository jobRepository) : IJobQueryService
{
    public async Task<PagedResult<JobOfferDto>> SearchAsync(JobSearchFilter filter, CancellationToken cancellationToken)
    {
        var result = await jobRepository.SearchAsync(filter, cancellationToken);
        var items = result.Items.Select(JobOfferDto.FromDomain).ToArray();

        return new PagedResult<JobOfferDto>(items, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<JobOfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken);
        return job is null ? null : JobOfferDto.FromDomain(job);
    }

    public async Task<IReadOnlyCollection<JobOfferHistoryEntryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var history = await jobRepository.GetHistoryAsync(id, cancellationToken);
        return history.Select(JobOfferHistoryEntryDto.FromDomain).ToArray();
    }
}
