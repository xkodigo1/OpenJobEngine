namespace OpenJobEngine.Application.Matching;

public sealed record MatchingRuleSetDto(
    string Version,
    SkillMatchingRulesDto Skills,
    ScoreWeightRulesDto Weights,
    ScorePenaltyRulesDto Penalties);

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
    decimal MissingSkillSignalsPenalty);
