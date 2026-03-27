using System.Globalization;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Providers.Lever;

public static class LeverJobMapper
{
    public static RawJobOffer Map(string sourceName, string site, string? companyName, LeverJobPostingsResponse job, string baseUrl)
    {
        var location = job.Categories.Location
            ?? job.Categories.AllLocations?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Text))?.Text;

        var description = CombineDescriptions(job);
        var hostedUrl = NormalizeUrl(job.HostedUrl) ?? NormalizeUrl(job.ApplyUrl) ?? BuildHostedUrl(baseUrl, site, job.Id);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(metadata, "site", site);
        Add(metadata, "location", location);
        Add(metadata, "commitment", job.Categories.Commitment);
        Add(metadata, "team", job.Categories.Team);
        Add(metadata, "department", job.Categories.Department);
        Add(metadata, "country", job.Country);
        Add(metadata, "workplace_type", job.WorkplaceType);
        Add(metadata, "apply_url", job.ApplyUrl);
        Add(metadata, "hosted_url", job.HostedUrl);
        Add(metadata, "salary_interval", job.SalaryRange?.Interval);
        Add(metadata, "salary_description", job.SalaryDescriptionPlain);
        Add(metadata, "lists", string.Join(" | ", job.Lists?.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)) ?? []));

        return new RawJobOffer(
            sourceName,
            job.Id,
            job.Text,
            InferCompanyName(site, companyName),
            description,
            location,
            BuildSalaryText(job.SalaryRange, job.SalaryDescriptionPlain),
            hostedUrl,
            null,
            metadata);
    }

    private static string CombineDescriptions(LeverJobPostingsResponse job)
    {
        var sections = new[]
        {
            StripHtml(job.OpeningPlain),
            StripHtml(job.DescriptionBodyPlain),
            StripHtml(job.AdditionalPlain),
            StripHtml(job.DescriptionPlain)
        };

        return string.Join(Environment.NewLine + Environment.NewLine, sections.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            .Trim();
    }

    private static string? BuildSalaryText(LeverSalaryRangeContract? salaryRange, string? salaryDescription)
    {
        if (salaryRange is null)
        {
            return string.IsNullOrWhiteSpace(salaryDescription) ? null : salaryDescription.Trim();
        }

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(salaryRange.Currency))
        {
            builder.Append(salaryRange.Currency!.Trim());
            builder.Append(' ');
        }

        if (salaryRange.Min.HasValue || salaryRange.Max.HasValue)
        {
            builder.Append(salaryRange.Min?.ToString("0.##", CultureInfo.InvariantCulture) ?? "?");
            builder.Append(" - ");
            builder.Append(salaryRange.Max?.ToString("0.##", CultureInfo.InvariantCulture) ?? "?");
        }
        else if (!string.IsNullOrWhiteSpace(salaryDescription))
        {
            builder.Append(salaryDescription.Trim());
        }

        if (!string.IsNullOrWhiteSpace(salaryRange.Interval))
        {
            builder.Append(' ');
            builder.Append('(');
            builder.Append(salaryRange.Interval!.Trim());
            builder.Append(')');
        }

        var value = builder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(salaryDescription))
        {
            value = string.IsNullOrWhiteSpace(value) ? salaryDescription.Trim() : $"{value} | {salaryDescription.Trim()}";
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string InferCompanyName(string site, string? companyName)
    {
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            return companyName.Trim();
        }

        var normalized = site.Replace("-", " ", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Lever Company";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());
    }

    private static string StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!value.Contains('<'))
        {
            return WebUtility.HtmlDecode(value).Trim();
        }

        var document = new HtmlDocument();
        document.LoadHtml(WebUtility.HtmlDecode(value));
        return WebUtility.HtmlDecode(document.DocumentNode.InnerText).Trim();
    }

    private static string? NormalizeUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.ToString() : null;
    }

    private static string BuildHostedUrl(string baseUrl, string site, string postingId)
    {
        var apiHost = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;
        var hostedHost = apiHost.Contains("api.eu.lever.co", StringComparison.OrdinalIgnoreCase)
            ? "jobs.eu.lever.co"
            : "jobs.lever.co";

        return $"https://{hostedHost}/{Uri.EscapeDataString(site)}/{Uri.EscapeDataString(postingId)}";
    }

    private static void Add(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }
}
