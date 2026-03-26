using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;

namespace OpenJobEngine.Application.Common;

public sealed class SystemMetricsService(
    IJobRepository jobRepository,
    ICandidateProfileRepository candidateProfileRepository,
    IMatchExecutionRepository matchExecutionRepository) : ISystemMetricsService
{
    public async Task<SystemMetricsDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var totalJobs = await jobRepository.CountAsync(false, cancellationToken);
        var activeJobs = await jobRepository.CountAsync(true, cancellationToken);
        var totalProfiles = await candidateProfileRepository.CountAsync(cancellationToken);
        var providerMetrics = await jobRepository.GetProviderQualityMetricsAsync(cancellationToken);

        return new SystemMetricsDto(
            totalJobs,
            activeJobs,
            totalProfiles,
            await candidateProfileRepository.CountAlertsAsync(cancellationToken),
            await candidateProfileRepository.CountSavedSearchesAsync(cancellationToken),
            await matchExecutionRepository.CountLastDaysAsync(7, cancellationToken),
            providerMetrics);
    }

    public Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProvidersAsync(CancellationToken cancellationToken)
    {
        return jobRepository.GetProviderQualityMetricsAsync(cancellationToken);
    }
}
