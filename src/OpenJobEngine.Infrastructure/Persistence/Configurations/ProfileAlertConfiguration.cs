using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class ProfileAlertConfiguration : IEntityTypeConfiguration<ProfileAlert>
{
    public void Configure(EntityTypeBuilder<ProfileAlert> builder)
    {
        builder.ToTable("profile_alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChannelType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Target).HasMaxLength(1000);
        builder.Property(x => x.MinimumMatchScore).HasPrecision(5, 2);
    }
}
