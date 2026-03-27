using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IAlertDeliveryRepository
{
    Task AddAsync(AlertDelivery delivery, CancellationToken cancellationToken);

    Task UpdateAsync(AlertDelivery delivery, CancellationToken cancellationToken);

    Task<AlertDelivery?> GetByAlertAndJobAsync(Guid alertId, Guid jobId, CancellationToken cancellationToken);

    Task<long> CountLastDaysAsync(int days, CancellationToken cancellationToken);

    Task<long> CountLastDaysByStatusAsync(int days, AlertDeliveryStatus status, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastDeliveredAtUtcAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AlertDelivery>> GetRecentAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AlertDelivery>> GetLastDaysAsync(int days, CancellationToken cancellationToken);
}
