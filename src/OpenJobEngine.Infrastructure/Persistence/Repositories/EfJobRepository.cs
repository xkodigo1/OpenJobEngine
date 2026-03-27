using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfJobRepository(OpenJobEngineDbContext dbContext) : IJobRepository
{
    private static string NormalizeQueryTerm(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private IQueryable<JobOffer> QueryWithDetails()
    {
        return dbContext.JobOffers
            .AsSplitQuery()
            .Include(x => x.SkillTags)
            .Include(x => x.LanguageRequirements);
    }

    public async Task AddAsync(JobOffer job, CancellationToken cancellationToken)
    {
        await dbContext.JobOffers.AddAsync(job, cancellationToken);
    }

    public Task UpdateAsync(JobOffer job, CancellationToken cancellationToken)
    {
        dbContext.JobOffers.Update(job);
        return Task.CompletedTask;
    }

    public Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return QueryWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<JobOffer?> GetByDedupKeyAsync(string key, CancellationToken cancellationToken)
    {
        return QueryWithDetails()
            .Include(x => x.SourceObservations)
            .FirstOrDefaultAsync(x => x.DeduplicationKey == key, cancellationToken);
    }

    public Task<JobOffer?> GetBySourceAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken)
    {
        return QueryWithDetails()
            .Include(x => x.SourceObservations)
            .FirstOrDefaultAsync(
                x => x.SourceName == sourceName && x.SourceJobId == sourceJobId,
                cancellationToken);
    }

    public Task<JobOfferSourceObservation?> GetObservationAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken)
    {
        return dbContext.JobOfferSourceObservations
            .FirstOrDefaultAsync(
                x => x.SourceName == sourceName && x.SourceJobId == sourceJobId,
                cancellationToken);
    }

    public async Task AddObservationAsync(JobOfferSourceObservation observation, CancellationToken cancellationToken)
    {
        await dbContext.JobOfferSourceObservations.AddAsync(observation, cancellationToken);
    }

    public Task UpdateObservationAsync(JobOfferSourceObservation observation, CancellationToken cancellationToken)
    {
        dbContext.JobOfferSourceObservations.Update(observation);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<JobOfferSourceObservation>> GetActiveObservationsBySourceAsync(string sourceName, CancellationToken cancellationToken)
    {
        return await dbContext.JobOfferSourceObservations
            .AsNoTracking()
            .Where(x => x.IsActive && x.SourceName == sourceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<JobOfferSourceObservation>> GetObservationsByJobIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await dbContext.JobOfferSourceObservations
            .Where(x => x.JobOfferId == jobId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddHistoryEntryAsync(JobOfferHistoryEntry entry, CancellationToken cancellationToken)
    {
        await dbContext.JobOfferHistoryEntries.AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyCollection<JobOfferHistoryEntry>> GetHistoryAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var entries = await dbContext.JobOfferHistoryEntries
            .AsNoTracking()
            .Where(x => x.JobOfferId == jobId)
            .ToListAsync(cancellationToken);

        return entries
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToArray();
    }

    public async Task<PagedResult<JobOffer>> SearchAsync(JobSearchFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var query = QueryWithDetails().AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = NormalizeQueryTerm(filter.Query);
            query = query.Where(x =>
                x.Title.ToLower().Contains(pattern) ||
                x.CompanyName.ToLower().Contains(pattern) ||
                (x.Description != null && x.Description.ToLower().Contains(pattern)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var pattern = NormalizeQueryTerm(filter.Location);
            query = query.Where(x =>
                (x.LocationText != null && x.LocationText.ToLower().Contains(pattern)) ||
                (x.City != null && x.City.ToLower().Contains(pattern)) ||
                (x.Region != null && x.Region.ToLower().Contains(pattern)));
        }

        if (filter.Remote.HasValue)
        {
            query = query.Where(x => x.IsRemote == filter.Remote.Value);
        }

        if (filter.SalaryMin.HasValue)
        {
            query = query.Where(x => x.SalaryMax == null || x.SalaryMax >= filter.SalaryMin.Value);
        }

        if (filter.SalaryMax.HasValue)
        {
            query = query.Where(x => x.SalaryMin == null || x.SalaryMin <= filter.SalaryMax.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            var source = NormalizeQueryTerm(filter.Source);
            query = query.Where(x => x.SourceName.ToLower().Contains(source));
        }

        var items = await query
            .ToListAsync(cancellationToken);

        var totalCount = items.LongCount();
        var pagedItems = items
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CollectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedResult<JobOffer>(pagedItems, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyCollection<JobOffer>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var items = await QueryWithDetails()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CollectedAtUtc)
            .ToArray();
    }

    public Task<long> CountAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.JobOffers.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return query.LongCountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProviderQualityMetricsDto>> GetProviderQualityMetricsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await dbContext.JobOfferSourceObservations
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Join(
                dbContext.JobOffers.AsNoTracking(),
                observation => observation.JobOfferId,
                job => job.Id,
                (observation, job) => new
                {
                    observation.SourceName,
                    observation.LastSeenAtUtc,
                    job.QualityScore,
                    job.SalaryMin,
                    job.SalaryMax,
                    job.SalaryCurrency,
                    job.QualityFlags,
                    HasLocation = job.CountryCode != null || job.City != null || job.Region != null,
                    HasSkills = dbContext.JobOfferSkillTags.Any(tag => tag.JobOfferId == job.Id),
                    HasLanguages = dbContext.JobOfferLanguageRequirements.Any(language => language.JobOfferId == job.Id)
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var total = group.Count();
                return new ProviderQualityMetricsDto(
                    group.Key,
                    total,
                    total == 0 ? 0m : decimal.Round(group.Average(x => x.QualityScore), 2),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.SalaryMin != null || x.SalaryMax != null) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x =>
                        (x.SalaryMin != null || x.SalaryMax != null) &&
                        !string.IsNullOrWhiteSpace(x.SalaryCurrency) &&
                        !ContainsQualityFlag(x.QualityFlags, "salary_amount_ambiguous") &&
                        !ContainsQualityFlag(x.QualityFlags, "salary_period_unsupported") &&
                        !ContainsQualityFlag(x.QualityFlags, "salary_amount_outlier")) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasLocation) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasSkills) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasLanguages) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.QualityScore < 0.55m) / (decimal)total, 4),
                    total == 0 ? null : decimal.Round(group.Average(x => (decimal)Math.Max(0, (now - x.LastSeenAtUtc).TotalHours)), 2));
            })
            .OrderByDescending(x => x.TotalActiveJobs)
            .ToArray();
    }

    private static bool ContainsQualityFlag(string? qualityFlags, string value)
    {
        return !string.IsNullOrWhiteSpace(qualityFlags) &&
               qualityFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Any(flag => string.Equals(flag, value, StringComparison.OrdinalIgnoreCase));
    }
}
