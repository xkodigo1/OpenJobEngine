using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class CandidateSkillConfiguration : IEntityTypeConfiguration<CandidateSkill>
{
    public void Configure(EntityTypeBuilder<CandidateSkill> builder)
    {
        builder.ToTable("candidate_skills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkillName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SkillSlug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SkillCategory).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.YearsExperience).HasPrecision(5, 2);

        builder.HasIndex(x => new { x.CandidateProfileId, x.SkillSlug });
    }
}
