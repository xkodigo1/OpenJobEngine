using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfJobSourceRepository(OpenJobEngineDbContext dbContext) : IJobSourceRepository
{
    public Task<JobSource?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return dbContext.JobSources.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task AddAsync(JobSource source, CancellationToken cancellationToken)
    {
        await dbContext.JobSources.AddAsync(source, cancellationToken);
    }

    public Task UpdateAsync(JobSource source, CancellationToken cancellationToken)
    {
        dbContext.JobSources.Update(source);
        return Task.CompletedTask;
    }
}
