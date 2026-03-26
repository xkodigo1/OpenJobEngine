using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class JobSourceConfiguration : IEntityTypeConfiguration<JobSource>
{
    public void Configure(EntityTypeBuilder<JobSource> builder)
    {
        builder.ToTable("job_sources");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
