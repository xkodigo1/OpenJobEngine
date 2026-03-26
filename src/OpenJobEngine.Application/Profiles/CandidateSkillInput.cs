namespace OpenJobEngine.Application.Profiles;

public sealed record CandidateSkillInput(
    string SkillName,
    string SkillSlug,
    string Category,
    decimal? YearsExperience,
    int ProficiencyScore);
