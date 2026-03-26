using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IScrapeExecutionRepository
{
    Task AddAsync(ScrapeExecution execution, CancellationToken cancellationToken);

    Task UpdateAsync(ScrapeExecution execution, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ScrapeExecution>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
