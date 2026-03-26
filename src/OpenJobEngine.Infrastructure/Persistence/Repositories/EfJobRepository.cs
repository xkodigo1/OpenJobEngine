using Microsoft.EntityFrameworkCore;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Persistence.Repositories;

public sealed class EfJobRepository(OpenJobEngineDbContext dbContext) : IJobRepository
{
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
        return dbContext.JobOffers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<JobOffer?> GetByDedupKeyAsync(string key, CancellationToken cancellationToken)
    {
        return dbContext.JobOffers
            .FirstOrDefaultAsync(x => x.DeduplicationKey == key, cancellationToken);
    }

    public Task<JobOffer?> GetBySourceAsync(string sourceName, string sourceJobId, CancellationToken cancellationToken)
    {
        return dbContext.JobOffers
            .FirstOrDefaultAsync(
                x => x.SourceName == sourceName && x.SourceJobId == sourceJobId,
                cancellationToken);
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

        var query = dbContext.JobOffers.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = $"%{filter.Query.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, pattern) ||
                EF.Functions.ILike(x.CompanyName, pattern) ||
                (x.Description != null && EF.Functions.ILike(x.Description, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var pattern = $"%{filter.Location.Trim()}%";
            query = query.Where(x => x.LocationText != null && EF.Functions.ILike(x.LocationText, pattern));
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
            query = query.Where(x => EF.Functions.ILike(x.SourceName, filter.Source));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CollectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobOffer>(items, page, pageSize, totalCount);
    }
}
