using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfProfileAlertRepository(OpenJobEngineDbContext dbContext) : IProfileAlertRepository
{
    public async Task AddAsync(ProfileAlert alert, CancellationToken cancellationToken)
    {
        await dbContext.ProfileAlerts.AddAsync(alert, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProfileAlert>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ProfileAlerts
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(ProfileAlert alert, CancellationToken cancellationToken)
    {
        dbContext.ProfileAlerts.Update(alert);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ProfileAlerts.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<int> CountByChannelAsync(AlertChannelType channelType, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ProfileAlerts.AsNoTracking().Where(x => x.ChannelType == channelType);
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return query.CountAsync(cancellationToken);
    }
}
