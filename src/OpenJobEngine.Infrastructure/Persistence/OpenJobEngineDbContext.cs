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

    public DbSet<JobOfferSkillTag> JobOfferSkillTags => Set<JobOfferSkillTag>();

    public DbSet<JobOfferLanguageRequirement> JobOfferLanguageRequirements => Set<JobOfferLanguageRequirement>();

    public DbSet<JobOfferSourceObservation> JobOfferSourceObservations => Set<JobOfferSourceObservation>();

    public DbSet<JobOfferHistoryEntry> JobOfferHistoryEntries => Set<JobOfferHistoryEntry>();

    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();

    public DbSet<CandidateLanguage> CandidateLanguages => Set<CandidateLanguage>();

    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    public DbSet<ProfileAlert> ProfileAlerts => Set<ProfileAlert>();

    public DbSet<MatchExecution> MatchExecutions => Set<MatchExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JobOfferConfiguration());
        modelBuilder.ApplyConfiguration(new JobOfferSkillTagConfiguration());
        modelBuilder.ApplyConfiguration(new JobOfferLanguageRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new JobOfferSourceObservationConfiguration());
        modelBuilder.ApplyConfiguration(new JobOfferHistoryEntryConfiguration());
        modelBuilder.ApplyConfiguration(new JobSourceConfiguration());
        modelBuilder.ApplyConfiguration(new ScrapeExecutionConfiguration());
        modelBuilder.ApplyConfiguration(new CandidateProfileConfiguration());
        modelBuilder.ApplyConfiguration(new CandidateSkillConfiguration());
        modelBuilder.ApplyConfiguration(new CandidateLanguageConfiguration());
        modelBuilder.ApplyConfiguration(new SavedSearchConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileAlertConfiguration());
        modelBuilder.ApplyConfiguration(new MatchExecutionConfiguration());
    }
}
