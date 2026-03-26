using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Providers;

public interface IJobProvider
{
    string SourceName { get; }

    Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken);
}
