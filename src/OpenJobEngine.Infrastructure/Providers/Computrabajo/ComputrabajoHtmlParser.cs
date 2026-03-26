using System.Text.RegularExpressions;
using HtmlAgilityPack;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Providers.Computrabajo;

public sealed partial class ComputrabajoHtmlParser
{
    private static readonly Regex JobIdRegex = JobIdPattern();

    public IReadOnlyCollection<RawJobOffer> Parse(string sourceName, string html, string baseUrl)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var cards = document.DocumentNode.SelectNodes(
                "//article[contains(@class,'box_offer')] | //article[contains(@class,'box_offerLink')] | //div[contains(@class,'js_row')]")
            ?.ToList()
            ?? new List<HtmlNode>();

        var jobs = new List<RawJobOffer>();

        foreach (var card in cards)
        {
            var linkNode = card.SelectSingleNode(".//a[contains(@class,'js-o-link')] | .//h2//a | .//a[@href]");
            var title = Clean(linkNode?.InnerText);
            var href = linkNode?.GetAttributeValue("href", null);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            var company = Clean(card.SelectSingleNode(".//*[contains(@class,'it-blank')] | .//*[contains(@class,'company')]")?.InnerText);
            var location = Clean(card.SelectSingleNode(".//*[contains(@class,'fs16')] | .//*[contains(@class,'location')]")?.InnerText);
            var description = Clean(card.SelectSingleNode(".//*[contains(@class,'txt')] | .//*[contains(@class,'description')]")?.InnerText);
            var salary = Clean(card.SelectSingleNode(".//*[contains(@class,'salary')]")?.InnerText);
            var absoluteUrl = new Uri(new Uri(baseUrl), href).ToString();
            var sourceJobId = ResolveJobId(card, absoluteUrl);

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(card.GetAttributeValue("data-city", null)))
            {
                metadata["city"] = card.GetAttributeValue("data-city", string.Empty);
            }

            jobs.Add(new RawJobOffer(
                sourceName,
                sourceJobId,
                title,
                string.IsNullOrWhiteSpace(company) ? "Unknown" : company,
                string.IsNullOrWhiteSpace(description) ? null : description,
                string.IsNullOrWhiteSpace(location) ? null : location,
                string.IsNullOrWhiteSpace(salary) ? null : salary,
                absoluteUrl,
                null,
                metadata));
        }

        return jobs
            .GroupBy(x => x.SourceJobId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
    }

    private static string ResolveJobId(HtmlNode card, string absoluteUrl)
    {
        var candidates = new[]
        {
            card.GetAttributeValue("data-id", null),
            card.GetAttributeValue("id", null),
            absoluteUrl
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var match = JobIdRegex.Match(candidate);
            if (match.Success)
            {
                return match.Value;
            }
        }

        return absoluteUrl;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return HtmlEntity.DeEntitize(value).Trim();
    }

    [GeneratedRegex(@"\d{5,}", RegexOptions.Compiled)]
    private static partial Regex JobIdPattern();
}
