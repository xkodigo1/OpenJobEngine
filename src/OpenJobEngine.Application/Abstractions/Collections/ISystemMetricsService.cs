using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface ISystemMetricsService
{
    Task<SystemMetricsDto> GetOverviewAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProvidersAsync(CancellationToken cancellationToken);
}
