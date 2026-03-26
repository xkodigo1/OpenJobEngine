using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("saved_searches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Query).HasMaxLength(200);
        builder.Property(x => x.Location).HasMaxLength(100);
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.MinimumSalary).HasPrecision(12, 2);
        builder.Property(x => x.MinimumMatchScore).HasPrecision(5, 2);
    }
}
