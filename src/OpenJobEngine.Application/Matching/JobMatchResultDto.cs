using OpenJobEngine.Application.Jobs;

namespace OpenJobEngine.Application.Matching;

public sealed record JobMatchResultDto(
    JobOfferDto Job,
    decimal MatchScore,
    string MatchBand,
    string RuleVersion,
    IReadOnlyCollection<string> MatchReasons,
    IReadOnlyCollection<string> MissingRequirements,
    string SalaryFit,
    string LocationFit,
    string LanguageFit);
