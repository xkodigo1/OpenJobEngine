using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IMatchExecutionRepository
{
    Task AddAsync(MatchExecution execution, CancellationToken cancellationToken);

    Task<int> CountLastDaysAsync(int days, CancellationToken cancellationToken);

    Task<MatchExecution?> GetLatestForProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MatchExecution>> GetLastDaysAsync(int days, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MatchExecution>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
