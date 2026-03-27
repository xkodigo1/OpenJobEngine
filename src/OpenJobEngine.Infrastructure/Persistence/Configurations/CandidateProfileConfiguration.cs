using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> builder)
    {
        builder.ToTable("candidate_profiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TargetTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProfessionalSummary).HasMaxLength(4000);
        builder.Property(x => x.YearsOfExperience).HasPrecision(5, 2);
        builder.Property(x => x.SeniorityLevel).HasConversion<string>().HasMaxLength(50);

        builder.OwnsOne(x => x.Preferences, preferences =>
        {
            preferences.Property(x => x.PreferredPrimaryWorkMode).HasConversion<string>().HasColumnName("preferred_work_mode").HasMaxLength(50);
            preferences.Property(x => x.AcceptRemote).HasColumnName("accept_remote");
            preferences.Property(x => x.AcceptHybrid).HasColumnName("accept_hybrid");
            preferences.Property(x => x.AcceptOnSite).HasColumnName("accept_on_site");
        });

        builder.OwnsOne(x => x.SalaryExpectation, salary =>
        {
            salary.Property(x => x.MinAmount).HasColumnName("salary_expectation_min").HasPrecision(12, 2);
            salary.Property(x => x.MaxAmount).HasColumnName("salary_expectation_max").HasPrecision(12, 2);
            salary.Property(x => x.Currency).HasColumnName("salary_expectation_currency").HasMaxLength(16);
        });

        builder.OwnsOne(x => x.LocationPreference, location =>
        {
            location.Property(x => x.CurrentCity).HasColumnName("current_city").HasMaxLength(100);
            location.Property(x => x.CurrentRegion).HasColumnName("current_region").HasMaxLength(100);
            location.Property(x => x.CurrentCountryCode).HasColumnName("current_country_code").HasMaxLength(16);
            location.Property(x => x.TargetCitiesCsv).HasColumnName("target_cities").HasMaxLength(1000);
            location.Property(x => x.TargetCountriesCsv).HasColumnName("target_countries").HasMaxLength(500);
            location.Property(x => x.IsWillingToRelocate).HasColumnName("is_willing_to_relocate");
        });

        builder.Navigation(x => x.Preferences).IsRequired();
        builder.Navigation(x => x.SalaryExpectation).IsRequired();
        builder.Navigation(x => x.LocationPreference).IsRequired();

        builder.HasMany(x => x.Skills)
            .WithOne()
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Languages)
            .WithOne()
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SavedSearches)
            .WithOne()
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Alerts)
            .WithOne()
            .HasForeignKey(x => x.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Skills).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Languages).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.SavedSearches).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Alerts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
