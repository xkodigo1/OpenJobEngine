using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Infrastructure.Persistence.Configurations;

namespace OpenJobEngine.Infrastructure.Persistence;

public sealed class OpenJobEngineDbContext(DbContextOptions<OpenJobEngineDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();

    public DbSet<JobSource> JobSources => Set<JobSource>();

    public DbSet<ScrapeExecution> ScrapeExecutions => Set<ScrapeExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobOfferConfiguration());
        modelBuilder.ApplyConfiguration(new JobSourceConfiguration());
        modelBuilder.ApplyConfiguration(new ScrapeExecutionConfiguration());
    }
}
