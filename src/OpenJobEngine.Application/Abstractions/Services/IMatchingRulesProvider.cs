using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IMatchingRulesProvider
{
    MatchingRuleSetDto GetCurrent();
}
