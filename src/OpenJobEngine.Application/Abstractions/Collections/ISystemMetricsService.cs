using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface ISystemMetricsService
{
    Task<SystemMetricsDto> GetOverviewAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProvidersAsync(CancellationToken cancellationToken);

    Task<MatchingMetricsDto> GetMatchingAsync(CancellationToken cancellationToken);

    Task<AlertMetricsDto> GetAlertsAsync(CancellationToken cancellationToken);

    Task<ProviderOperationsMetricsDto> GetProviderOperationsAsync(CancellationToken cancellationToken);
}
