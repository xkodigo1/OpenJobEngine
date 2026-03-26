using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class CandidateSkill
{
    private CandidateSkill()
    {
    }

    public CandidateSkill(Guid id, Guid candidateProfileId, string skillName, string skillSlug, SkillCategory skillCategory, decimal? yearsExperience, int proficiencyScore)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        SkillName = skillName;
        SkillSlug = skillSlug;
        SkillCategory = skillCategory;
        YearsExperience = yearsExperience;
        ProficiencyScore = proficiencyScore;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string SkillName { get; private set; } = string.Empty;

    public string SkillSlug { get; private set; } = string.Empty;

    public SkillCategory SkillCategory { get; private set; }

    public decimal? YearsExperience { get; private set; }

    public int ProficiencyScore { get; private set; }

    public CandidateSkill CloneForProfile(Guid candidateProfileId)
    {
        return new CandidateSkill(Guid.NewGuid(), candidateProfileId, SkillName, SkillSlug, SkillCategory, YearsExperience, ProficiencyScore);
    }
}
