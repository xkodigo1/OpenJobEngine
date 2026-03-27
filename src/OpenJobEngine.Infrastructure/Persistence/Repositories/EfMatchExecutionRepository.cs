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
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        return dbContext.MatchExecutions.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= threshold, cancellationToken);
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
}
