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
}
