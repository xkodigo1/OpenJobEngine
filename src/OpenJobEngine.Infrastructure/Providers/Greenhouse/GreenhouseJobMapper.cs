using System.Globalization;
using System.Net;
using HtmlAgilityPack;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Providers.Greenhouse;

public static class GreenhouseJobMapper
{
    public static RawJobOffer Map(string sourceName, GreenhouseJobContract job)
    {
        var description = NormalizeContent(job.Content);
        var location = job.Location?.Name
            ?? job.Offices?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Location))?.Location
            ?? job.Offices?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name))?.Name;

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["location"] = location ?? string.Empty,
            ["language"] = job.Language ?? string.Empty,
            ["departments"] = string.Join(", ", job.Departments?.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)) ?? []),
            ["offices"] = string.Join(", ", job.Offices?.Select(x => x.Location ?? x.Name).Where(x => !string.IsNullOrWhiteSpace(x)) ?? [])
        };

        return new RawJobOffer(
            sourceName,
            job.Id.ToString(CultureInfo.InvariantCulture),
            job.Title,
            InferCompanyName(job.AbsoluteUrl),
            description,
            location,
            null,
            job.AbsoluteUrl,
            DateTimeOffset.TryParse(job.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var updatedAtUtc) ? updatedAtUtc : null,
            metadata);
    }

    private static string NormalizeContent(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var document = new HtmlDocument();
        document.LoadHtml(WebUtility.HtmlDecode(html));
        return WebUtility.HtmlDecode(document.DocumentNode.InnerText).Trim();
    }

    private static string InferCompanyName(string absoluteUrl)
    {
        if (Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host.Replace("boards.greenhouse.io", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace('.', ' ')
                .Trim();
        }

        return "Greenhouse Company";
    }
}
