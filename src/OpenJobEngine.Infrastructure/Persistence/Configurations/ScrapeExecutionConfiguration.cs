using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class ScrapeExecutionConfiguration : IEntityTypeConfiguration<ScrapeExecution>
{
    public void Configure(EntityTypeBuilder<ScrapeExecution> builder)
    {
        builder.ToTable("scrape_executions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.SourceName);
        builder.HasIndex(x => x.StartedAtUtc);
    }
}
