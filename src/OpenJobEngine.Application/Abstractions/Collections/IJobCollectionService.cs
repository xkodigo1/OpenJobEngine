using OpenJobEngine.Application.Collections;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IJobCollectionService
{
    Task<CollectionRunResultDto> RunAllAsync(CancellationToken cancellationToken);

    Task<CollectionRunResultDto> RunSourceAsync(string sourceName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ScrapeExecutionDto>> GetRecentExecutionsAsync(int take, CancellationToken cancellationToken);
}
