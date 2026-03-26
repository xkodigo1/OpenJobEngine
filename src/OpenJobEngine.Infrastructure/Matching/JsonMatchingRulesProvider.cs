using System.Text.Json;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Matching;

namespace OpenJobEngine.Infrastructure.Matching;

public sealed class JsonMatchingRulesProvider : IMatchingRulesProvider
{
    private readonly Lazy<MatchingRuleSetDto> rules;

    public JsonMatchingRulesProvider()
    {
        rules = new Lazy<MatchingRuleSetDto>(LoadRules, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public MatchingRuleSetDto GetCurrent() => rules.Value;

    private static MatchingRuleSetDto LoadRules()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Matching", "Data", "matching-rules.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Matching rules file was not found.", path);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var raw = JsonSerializer.Deserialize<MatchingRulesFile>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Matching rules file is empty or invalid.");

        return new MatchingRuleSetDto(
            raw.Version,
            new SkillMatchingRulesDto(raw.Skills.EquivalentSkillMultiplier, raw.Skills.RelatedSkillMultiplier),
            new ScoreWeightRulesDto(
                raw.Weights.RequiredSkills,
                raw.Weights.OptionalSkills,
                raw.Weights.Seniority,
                raw.Weights.ExperienceYears,
                raw.Weights.LocationWorkMode,
                raw.Weights.Salary,
                raw.Weights.Languages),
            new ScorePenaltyRulesDto(
                raw.Penalties.MaxQualityPenalty,
                raw.Penalties.LowQualityThreshold,
                raw.Penalties.MissingSkillSignalsPenalty));
    }

    private sealed record MatchingRulesFile(string Version, SkillRulesFile Skills, WeightRulesFile Weights, PenaltyRulesFile Penalties);

    private sealed record SkillRulesFile(decimal EquivalentSkillMultiplier, decimal RelatedSkillMultiplier);

    private sealed record WeightRulesFile(
        decimal RequiredSkills,
        decimal OptionalSkills,
        decimal Seniority,
        decimal ExperienceYears,
        decimal LocationWorkMode,
        decimal Salary,
        decimal Languages);

    private sealed record PenaltyRulesFile(decimal MaxQualityPenalty, decimal LowQualityThreshold, decimal MissingSkillSignalsPenalty);
}
