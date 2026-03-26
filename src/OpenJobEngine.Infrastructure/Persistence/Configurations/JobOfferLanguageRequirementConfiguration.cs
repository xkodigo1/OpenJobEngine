using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobOfferLanguageRequirementConfiguration : IEntityTypeConfiguration<JobOfferLanguageRequirement>
{
    public void Configure(EntityTypeBuilder<JobOfferLanguageRequirement> builder)
    {
        builder.ToTable("job_offer_language_requirements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LanguageName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MinimumProficiency).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 2);

        builder.HasIndex(x => new { x.JobOfferId, x.LanguageCode });
    }
}
