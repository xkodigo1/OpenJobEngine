using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IProfileAlertRepository
{
    Task AddAsync(ProfileAlert alert, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProfileAlert>> ListActiveAsync(CancellationToken cancellationToken);

    Task UpdateAsync(ProfileAlert alert, CancellationToken cancellationToken);

    Task<int> CountAsync(bool activeOnly, CancellationToken cancellationToken);

    Task<int> CountByChannelAsync(AlertChannelType channelType, bool activeOnly, CancellationToken cancellationToken);
}
