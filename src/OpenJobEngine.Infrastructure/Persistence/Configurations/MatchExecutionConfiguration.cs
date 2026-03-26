using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Configurations;

public sealed class MatchExecutionConfiguration : IEntityTypeConfiguration<MatchExecution>
{
    public void Configure(EntityTypeBuilder<MatchExecution> builder)
    {
        builder.ToTable("match_executions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Query).HasMaxLength(200);
        builder.Property(x => x.TopScore).HasPrecision(5, 2);
        builder.Property(x => x.RuleVersion).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.CandidateProfileId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
