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
        return await dbContext.JobOfferHistoryEntries
            .AsNoTracking()
            .Where(x => x.JobOfferId == jobId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
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

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CollectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobOffer>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyCollection<JobOffer>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await QueryWithDetails()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CollectedAtUtc)
            .ToListAsync(cancellationToken);
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
                    job.QualityScore,
                    HasSalary = job.SalaryMin != null || job.SalaryMax != null,
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
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasSalary) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasLocation) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasSkills) / (decimal)total, 4),
                    total == 0 ? 0m : decimal.Round(group.Count(x => x.HasLanguages) / (decimal)total, 4));
            })
            .OrderByDescending(x => x.TotalActiveJobs)
            .ToArray();
    }
}
