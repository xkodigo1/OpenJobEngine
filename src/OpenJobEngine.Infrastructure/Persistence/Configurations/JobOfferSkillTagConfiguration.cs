using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobOfferSkillTagConfiguration : IEntityTypeConfiguration<JobOfferSkillTag>
{
    public void Configure(EntityTypeBuilder<JobOfferSkillTag> builder)
    {
        builder.ToTable("job_offer_skill_tags");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkillName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SkillSlug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SkillCategory).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 2);

        builder.HasIndex(x => new { x.JobOfferId, x.SkillSlug });
    }
}
