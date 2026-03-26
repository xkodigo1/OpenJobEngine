using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Providers.Adzuna;

public static class AdzunaJobMapper
{
    public static RawJobOffer Map(string sourceName, AdzunaJobContract job)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(metadata, "contract_time", job.ContractTime);
        Add(metadata, "contract_type", job.ContractType);
        Add(metadata, "category", job.Category?.DisplayName);

        var salaryText = job.SalaryMin.HasValue || job.SalaryMax.HasValue
            ? $"{job.SalaryMin?.ToString("0.##")}-{job.SalaryMax?.ToString("0.##")}"
            : null;

        return new RawJobOffer(
            sourceName,
            job.Id,
            job.Title,
            job.Company?.DisplayName ?? "Unknown",
            job.Description,
            job.Location?.DisplayName,
            salaryText,
            job.RedirectUrl,
            DateTimeOffset.TryParse(job.Created, out var publishedAtUtc) ? publishedAtUtc : null,
            metadata);
    }

    private static void Add(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }
}
