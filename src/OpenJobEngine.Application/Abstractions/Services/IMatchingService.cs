using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IMatchingService
{
    Task<MatchingSearchResultDto> SearchAsync(MatchingSearchRequest request, CancellationToken cancellationToken);

    Task<JobMatchResultDto?> GetJobMatchAsync(Guid profileId, Guid jobId, CancellationToken cancellationToken);

    MatchingRuleSetDto GetCurrentRules();
}
