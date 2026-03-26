using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class CandidateLanguageConfiguration : IEntityTypeConfiguration<CandidateLanguage>
{
    public void Configure(EntityTypeBuilder<CandidateLanguage> builder)
    {
        builder.ToTable("candidate_languages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LanguageName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Proficiency).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => new { x.CandidateProfileId, x.LanguageCode });
    }
}
