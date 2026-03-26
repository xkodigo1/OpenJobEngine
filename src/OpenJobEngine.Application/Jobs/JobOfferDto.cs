using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Jobs;

public sealed record JobOfferDto(
    Guid Id,
    string Title,
    string CompanyName,
    string? Description,
    string? LocationText,
    string EmploymentType,
    string SeniorityLevel,
    string? SalaryText,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    bool IsRemote,
    string Url,
    string SourceName,
    string SourceJobId,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CollectedAtUtc,
    string DeduplicationKey,
    bool IsActive)
{
    public static JobOfferDto FromDomain(JobOffer job)
    {
        return new JobOfferDto(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Description,
            job.LocationText,
            job.EmploymentType.ToString(),
            job.SeniorityLevel.ToString(),
            job.SalaryText,
            job.SalaryMin,
            job.SalaryMax,
            job.SalaryCurrency,
            job.IsRemote,
            job.Url,
            job.SourceName,
            job.SourceJobId,
            job.PublishedAtUtc,
            job.CollectedAtUtc,
            job.DeduplicationKey,
            job.IsActive);
    }
}
