using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfScrapeExecutionRepository(OpenJobEngineDbContext dbContext) : IScrapeExecutionRepository
{
    public async Task AddAsync(ScrapeExecution execution, CancellationToken cancellationToken)
    {
        await dbContext.ScrapeExecutions.AddAsync(execution, cancellationToken);
    }

    public Task UpdateAsync(ScrapeExecution execution, CancellationToken cancellationToken)
    {
        dbContext.ScrapeExecutions.Update(execution);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<ScrapeExecution>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var items = await dbContext.ScrapeExecutions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(take <= 0 ? 20 : take)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<ScrapeExecution>> GetLastDaysAsync(int days, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        var items = await dbContext.ScrapeExecutions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .Where(x => x.StartedAtUtc >= threshold)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToArray();
    }
}
