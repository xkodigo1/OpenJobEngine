using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobOfferSourceObservationConfiguration : IEntityTypeConfiguration<JobOfferSourceObservation>
{
    public void Configure(EntityTypeBuilder<JobOfferSourceObservation> builder)
    {
        builder.ToTable("job_offer_source_observations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceJobId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SnapshotHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.SourceName, x.SourceJobId }).IsUnique();
        builder.HasIndex(x => new { x.JobOfferId, x.IsActive });
    }
}
