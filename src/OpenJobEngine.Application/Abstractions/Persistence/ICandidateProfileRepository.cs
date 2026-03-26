using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface ICandidateProfileRepository
{
    Task AddAsync(CandidateProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(CandidateProfile profile, CancellationToken cancellationToken);

    Task<CandidateProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<int> CountSavedSearchesAsync(CancellationToken cancellationToken);

    Task<int> CountAlertsAsync(CancellationToken cancellationToken);
}
