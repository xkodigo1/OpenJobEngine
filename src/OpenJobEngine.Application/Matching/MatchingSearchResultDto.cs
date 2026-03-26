using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Matching;

public sealed record MatchingSearchResultDto(
    Guid ProfileId,
    string RuleVersion,
    PagedResult<JobMatchResultDto> Results);
