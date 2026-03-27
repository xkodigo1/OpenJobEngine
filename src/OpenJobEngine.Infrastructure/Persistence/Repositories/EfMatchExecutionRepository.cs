using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfMatchExecutionRepository(OpenJobEngineDbContext dbContext) : IMatchExecutionRepository
{
    public async Task AddAsync(MatchExecution execution, CancellationToken cancellationToken)
    {
        await dbContext.MatchExecutions.AddAsync(execution, cancellationToken);
    }

    public Task<int> CountLastDaysAsync(int days, CancellationToken cancellationToken)
    {
        return CountLastDaysCoreAsync(days, cancellationToken);
    }

    public async Task<MatchExecution?> GetLatestForProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var executions = await dbContext.MatchExecutions
            .AsNoTracking()
            .Where(x => x.CandidateProfileId == profileId)
            .ToListAsync(cancellationToken);

        return executions
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<MatchExecution>> GetLastDaysAsync(int days, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        var items = await dbContext.MatchExecutions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .Where(x => x.CreatedAtUtc >= threshold)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<MatchExecution>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var items = await dbContext.MatchExecutions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take <= 0 ? 20 : take)
            .ToArray();
    }

    private async Task<int> CountLastDaysCoreAsync(int days, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        var items = await dbContext.MatchExecutions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items.Count(x => x.CreatedAtUtc >= threshold);
    }
}
