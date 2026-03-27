using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class AlertDeliveryConfiguration : IEntityTypeConfiguration<AlertDelivery>
{
    public void Configure(EntityTypeBuilder<AlertDelivery> builder)
    {
        builder.ToTable("alert_deliveries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChannelType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.MatchScore).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.MatchBand).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RuleVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Target).HasMaxLength(1000);
        builder.Property(x => x.LastResponseBody).HasMaxLength(4000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ProfileAlertId, x.JobOfferId }).IsUnique();
        builder.HasIndex(x => x.DispatchedAtUtc);
        builder.HasIndex(x => x.Status);
    }
}
