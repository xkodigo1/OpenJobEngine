using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobOfferHistoryEntryConfiguration : IEntityTypeConfiguration<JobOfferHistoryEntry>
{
    public void Configure(EntityTypeBuilder<JobOfferHistoryEntry> builder)
    {
        builder.ToTable("job_offer_history_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SnapshotHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.SourceName).HasMaxLength(100);

        builder.HasIndex(x => new { x.JobOfferId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.JobOfferId, x.EventType });
    }
}
