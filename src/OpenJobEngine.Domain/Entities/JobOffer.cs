using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class JobOffer
{
    private JobOffer()
    {
    }

    public JobOffer(
        Guid id,
        string title,
        string companyName,
        string? description,
        string? locationText,
        EmploymentType employmentType,
        SeniorityLevel seniorityLevel,
        string? salaryText,
        decimal? salaryMin,
        decimal? salaryMax,
        string? salaryCurrency,
        bool isRemote,
        string url,
        string sourceName,
        string sourceJobId,
        DateTimeOffset? publishedAtUtc,
        DateTimeOffset collectedAtUtc,
        string deduplicationKey,
        bool isActive)
    {
        Id = id;
        Title = title;
        CompanyName = companyName;
        Description = description;
        LocationText = locationText;
        EmploymentType = employmentType;
        SeniorityLevel = seniorityLevel;
        SalaryText = salaryText;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryCurrency = salaryCurrency;
        IsRemote = isRemote;
        Url = url;
        SourceName = sourceName;
        SourceJobId = sourceJobId;
        PublishedAtUtc = publishedAtUtc;
        CollectedAtUtc = collectedAtUtc;
        DeduplicationKey = deduplicationKey;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? LocationText { get; private set; }

    public EmploymentType EmploymentType { get; private set; }

    public SeniorityLevel SeniorityLevel { get; private set; }

    public string? SalaryText { get; private set; }

    public decimal? SalaryMin { get; private set; }

    public decimal? SalaryMax { get; private set; }

    public string? SalaryCurrency { get; private set; }

    public bool IsRemote { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public string SourceName { get; private set; } = string.Empty;

    public string SourceJobId { get; private set; } = string.Empty;

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset CollectedAtUtc { get; private set; }

    public string DeduplicationKey { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void RefreshFrom(JobOffer incoming, string deduplicationKey, bool preserveSourceIdentity = false)
    {
        Title = incoming.Title;
        CompanyName = incoming.CompanyName;
        Description = incoming.Description;
        LocationText = incoming.LocationText;
        EmploymentType = incoming.EmploymentType;
        SeniorityLevel = incoming.SeniorityLevel;
        SalaryText = incoming.SalaryText;
        SalaryMin = incoming.SalaryMin;
        SalaryMax = incoming.SalaryMax;
        SalaryCurrency = incoming.SalaryCurrency;
        IsRemote = incoming.IsRemote;
        if (!preserveSourceIdentity)
        {
            Url = incoming.Url;
            SourceName = incoming.SourceName;
            SourceJobId = incoming.SourceJobId;
        }

        PublishedAtUtc = incoming.PublishedAtUtc;
        CollectedAtUtc = incoming.CollectedAtUtc;
        DeduplicationKey = deduplicationKey;
        IsActive = true;
    }

    public void AssignDeduplicationKey(string key)
    {
        DeduplicationKey = key;
    }

    public void MarkInactive()
    {
        IsActive = false;
    }
}
