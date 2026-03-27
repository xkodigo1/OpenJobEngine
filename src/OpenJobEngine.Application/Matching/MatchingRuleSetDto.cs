namespace OpenJobEngine.Application.Matching;

public sealed record MatchingRuleSetDto(
    string Version,
    SkillMatchingRulesDto Skills,
    ScoreWeightRulesDto Weights,
    ScorePenaltyRulesDto Penalties,
    MatchHardRequirementRulesDto HardRequirements,
    MatchToleranceRulesDto Tolerances);

public sealed record SkillMatchingRulesDto(
    decimal EquivalentSkillMultiplier,
    decimal RelatedSkillMultiplier);

public sealed record ScoreWeightRulesDto(
    decimal RequiredSkills,
    decimal OptionalSkills,
    decimal Seniority,
    decimal ExperienceYears,
    decimal LocationWorkMode,
    decimal Salary,
    decimal Languages);

public sealed record ScorePenaltyRulesDto(
    decimal MaxQualityPenalty,
    decimal LowQualityThreshold,
    decimal MissingSkillSignalsPenalty,
    decimal HardFailurePenalty,
    decimal ExcludedWorkModePenalty,
    decimal CompanyKeywordExclusionPenalty,
    decimal TimezoneMismatchPenalty);

public sealed record MatchHardRequirementRulesDto(
    decimal MinimumRequiredSkillsCoverage,
    decimal MinimumLanguageCoverage);

public sealed record MatchToleranceRulesDto(
    decimal SalaryCloseMatchRatio,
    decimal CompanyKeywordInclusionBoost,
    decimal TimezoneMatchBoost,
    decimal NewHighPriorityThreshold);
