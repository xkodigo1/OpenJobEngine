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
        builder.Property(x => x.SalaryText).HasMaxLength(200);
        builder.Property(x => x.SalaryCurrency).HasMaxLength(16);
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceJobId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.SeniorityLevel).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => new { x.SourceName, x.SourceJobId }).IsUnique();
        builder.HasIndex(x => x.DeduplicationKey);
        builder.HasIndex(x => x.Title);
        builder.HasIndex(x => x.CompanyName);
    }
}
