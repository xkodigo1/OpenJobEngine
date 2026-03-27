using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IMatchingService
{
    Task<MatchingSearchResultDto> SearchAsync(MatchingSearchRequest request, CancellationToken cancellationToken);

    Task<JobMatchResultDto?> GetJobMatchAsync(Guid profileId, Guid jobId, CancellationToken cancellationToken);

    Task<MatchingSearchResultDto> GetNewHighPriorityMatchesAsync(
        Guid profileId,
        decimal? minimumMatchScore,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<JobMatchResultDto>> GetAlertCandidatesAsync(
        Guid profileId,
        decimal? minimumMatchScore,
        bool onlyNewJobs,
        DateTimeOffset? baselineUtc,
        CancellationToken cancellationToken);

    MatchingRuleSetDto GetCurrentRules();
}
