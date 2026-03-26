using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Jobs;

public sealed record JobOfferDto(
    Guid Id,
    string Title,
    string CompanyName,
    string? Description,
    string? LocationText,
    string WorkMode,
    string? City,
    string? Region,
    string? CountryCode,
    string? TimeZone,
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
    DateTimeOffset LastSeenAtUtc,
    string DeduplicationKey,
    bool IsActive,
    decimal QualityScore,
    IReadOnlyCollection<string> QualityFlags,
    IReadOnlyCollection<JobSkillTagDto> SkillTags,
    IReadOnlyCollection<JobLanguageRequirementDto> LanguageRequirements)
{
    public static JobOfferDto FromDomain(JobOffer job)
    {
        return new JobOfferDto(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Description,
            job.LocationText,
            job.WorkMode.ToString(),
            job.City,
            job.Region,
            job.CountryCode,
            job.TimeZone,
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
            job.LastSeenAtUtc,
            job.DeduplicationKey,
            job.IsActive,
            job.QualityScore,
            job.GetQualityFlags(),
            job.SkillTags.Select(JobSkillTagDto.FromDomain).ToArray(),
            job.LanguageRequirements.Select(JobLanguageRequirementDto.FromDomain).ToArray());
    }
}

public sealed record JobSkillTagDto(
    string SkillName,
    string SkillSlug,
    string SkillCategory,
    bool IsRequired,
    decimal ConfidenceScore)
{
    public static JobSkillTagDto FromDomain(JobOfferSkillTag tag) =>
        new(tag.SkillName, tag.SkillSlug, tag.SkillCategory.ToString(), tag.IsRequired, tag.ConfidenceScore);
}

public sealed record JobLanguageRequirementDto(
    string LanguageCode,
    string LanguageName,
    string MinimumProficiency,
    bool IsRequired,
    decimal ConfidenceScore)
{
    public static JobLanguageRequirementDto FromDomain(JobOfferLanguageRequirement requirement) =>
        new(
            requirement.LanguageCode,
            requirement.LanguageName,
            requirement.MinimumProficiency.ToString(),
            requirement.IsRequired,
            requirement.ConfidenceScore);
}
