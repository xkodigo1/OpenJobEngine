using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfCandidateProfileRepository(OpenJobEngineDbContext dbContext) : ICandidateProfileRepository
{
    private IQueryable<CandidateProfile> QueryWithDetails()
    {
        return dbContext.CandidateProfiles
            .Include(x => x.Skills)
            .Include(x => x.Languages)
            .Include(x => x.SavedSearches)
            .Include(x => x.Alerts);
    }

    public async Task AddAsync(CandidateProfile profile, CancellationToken cancellationToken)
    {
        await dbContext.CandidateProfiles.AddAsync(profile, cancellationToken);
    }

    public Task UpdateAsync(CandidateProfile profile, CancellationToken cancellationToken)
    {
        dbContext.CandidateProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public Task<CandidateProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return QueryWithDetails().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return dbContext.CandidateProfiles.AsNoTracking().CountAsync(cancellationToken);
    }

    public Task<int> CountSavedSearchesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SavedSearches.AsNoTracking().CountAsync(cancellationToken);
    }

    public Task<int> CountAlertsAsync(CancellationToken cancellationToken)
    {
        return dbContext.ProfileAlerts.AsNoTracking().CountAsync(cancellationToken);
    }
}
