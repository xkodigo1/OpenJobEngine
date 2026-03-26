using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class JobOfferSkillTag
{
    private JobOfferSkillTag()
    {
    }

    public JobOfferSkillTag(Guid id, Guid jobOfferId, string skillName, string skillSlug, SkillCategory skillCategory, bool isRequired, decimal confidenceScore)
    {
        Id = id;
        JobOfferId = jobOfferId;
        SkillName = skillName;
        SkillSlug = skillSlug;
        SkillCategory = skillCategory;
        IsRequired = isRequired;
        ConfidenceScore = confidenceScore;
    }

    public Guid Id { get; private set; }

    public Guid JobOfferId { get; private set; }

    public string SkillName { get; private set; } = string.Empty;

    public string SkillSlug { get; private set; } = string.Empty;

    public SkillCategory SkillCategory { get; private set; }

    public bool IsRequired { get; private set; }

    public decimal ConfidenceScore { get; private set; }

    public JobOfferSkillTag CloneForJob(Guid jobOfferId)
    {
        return new JobOfferSkillTag(Guid.NewGuid(), jobOfferId, SkillName, SkillSlug, SkillCategory, IsRequired, ConfidenceScore);
    }
}
