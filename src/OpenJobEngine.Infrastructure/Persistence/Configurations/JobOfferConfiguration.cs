using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobOfferConfiguration : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("job_offers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LocationText).HasMaxLength(200);
        builder.Property(x => x.WorkMode).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.CountryCode).HasMaxLength(16);
        builder.Property(x => x.SalaryText).HasMaxLength(200);
        builder.Property(x => x.SalaryCurrency).HasMaxLength(16);
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceJobId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.QualityScore).HasPrecision(5, 2);
        builder.Property(x => x.QualityFlags).HasMaxLength(500);
        builder.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.SeniorityLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.LastSeenAtUtc).IsRequired();

        builder.HasMany<JobOfferSkillTag>("skillTags")
            .WithOne()
            .HasForeignKey(x => x.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<JobOfferLanguageRequirement>("languageRequirements")
            .WithOne()
            .HasForeignKey(x => x.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<JobOfferSourceObservation>("sourceObservations")
            .WithOne()
            .HasForeignKey(x => x.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<JobOfferHistoryEntry>("historyEntries")
            .WithOne()
            .HasForeignKey(x => x.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.SkillTags).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.LanguageRequirements).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.SourceObservations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.HistoryEntries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.SourceName, x.SourceJobId }).IsUnique();
        builder.HasIndex(x => x.DeduplicationKey);
        builder.HasIndex(x => x.Title);
        builder.HasIndex(x => x.CompanyName);
        builder.HasIndex(x => x.LastSeenAtUtc);
    }
}
