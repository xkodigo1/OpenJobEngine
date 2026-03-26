using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IJobRepository
{
    Task AddAsync(JobOffer job, CancellationToken cancellationToken);

    Task UpdateAsync(JobOffer job, CancellationToken cancellationToken);

    Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<JobOffer?> GetByDedupKeyAsync(string key, CancellationToken cancellationToken);

    Task<JobOffer?> GetBySourceAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken);

    Task<PagedResult<JobOffer>> SearchAsync(JobSearchFilter filter, CancellationToken cancellationToken);
}
