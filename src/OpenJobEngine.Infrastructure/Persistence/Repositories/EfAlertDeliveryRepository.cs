using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfAlertDeliveryRepository(OpenJobEngineDbContext dbContext) : IAlertDeliveryRepository
{
    public async Task AddAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        await dbContext.AlertDeliveries.AddAsync(delivery, cancellationToken);
    }

    public Task UpdateAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        // Deliveries are added or queried tracked within the current DbContext scope.
        // Forcing Update would turn newly-added entities into Modified and break inserts.
        return Task.CompletedTask;
    }

    public Task<AlertDelivery?> GetByAlertAndJobAsync(Guid alertId, Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.AlertDeliveries
            .FirstOrDefaultAsync(
                x => x.ProfileAlertId == alertId && x.JobOfferId == jobId,
                cancellationToken);
    }

    public Task<long> CountLastDaysAsync(int days, CancellationToken cancellationToken)
    {
        return CountLastDaysCoreAsync(days, null, cancellationToken);
    }

    public Task<long> CountLastDaysByStatusAsync(int days, AlertDeliveryStatus status, CancellationToken cancellationToken)
    {
        return CountLastDaysCoreAsync(days, status, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastDeliveredAtUtcAsync(CancellationToken cancellationToken)
    {
        var deliveredAtValues = await dbContext.AlertDeliveries
            .AsNoTracking()
            .Where(x => x.DeliveredAtUtc.HasValue)
            .Select(x => x.DeliveredAtUtc)
            .ToListAsync(cancellationToken);

        return deliveredAtValues
            .Where(x => x.HasValue)
            .OrderByDescending(x => x)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<AlertDelivery>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var items = await dbContext.AlertDeliveries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.DispatchedAtUtc)
            .Take(take <= 0 ? 20 : take)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AlertDelivery>> GetLastDaysAsync(int days, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        var items = await dbContext.AlertDeliveries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items
            .Where(x => x.DispatchedAtUtc >= threshold)
            .OrderByDescending(x => x.DispatchedAtUtc)
            .ToArray();
    }

    private async Task<long> CountLastDaysCoreAsync(
        int days,
        AlertDeliveryStatus? status,
        CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days <= 0 ? 7 : days));
        var items = await dbContext.AlertDeliveries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items.LongCount(x =>
            x.DispatchedAtUtc >= threshold &&
            (!status.HasValue || x.Status == status.Value));
    }
}
