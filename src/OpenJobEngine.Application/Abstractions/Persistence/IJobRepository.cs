using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IJobRepository
{
    Task AddAsync(JobOffer job, CancellationToken cancellationToken);

    Task UpdateAsync(JobOffer job, CancellationToken cancellationToken);

    Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<JobOffer?> GetByDedupKeyAsync(string key, CancellationToken cancellationToken);

    Task<JobOffer?> GetBySourceAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken);

    Task<JobOfferSourceObservation?> GetObservationAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken);

    Task AddObservationAsync(JobOfferSourceObservation observation, CancellationToken cancellationToken);

    Task UpdateObservationAsync(JobOfferSourceObservation observation, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<JobOfferSourceObservation>> GetActiveObservationsBySourceAsync(string sourceName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<JobOfferSourceObservation>> GetObservationsByJobIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task AddHistoryEntryAsync(JobOfferHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<JobOfferHistoryEntry>> GetHistoryAsync(Guid jobId, CancellationToken cancellationToken);

    Task<PagedResult<JobOffer>> SearchAsync(JobSearchFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<JobOffer>> ListActiveAsync(CancellationToken cancellationToken);

    Task<long> CountAsync(bool activeOnly, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProviderQualityMetricsAsync(CancellationToken cancellationToken);
}
