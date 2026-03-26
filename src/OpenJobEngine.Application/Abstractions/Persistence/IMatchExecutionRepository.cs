using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IMatchExecutionRepository
{
    Task AddAsync(MatchExecution execution, CancellationToken cancellationToken);

    Task<int> CountLastDaysAsync(int days, CancellationToken cancellationToken);
}
