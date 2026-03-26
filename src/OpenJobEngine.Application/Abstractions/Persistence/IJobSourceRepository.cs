using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Persistence;

public interface IJobSourceRepository
{
    Task<JobSource?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(JobSource source, CancellationToken cancellationToken);

    Task UpdateAsync(JobSource source, CancellationToken cancellationToken);
}
