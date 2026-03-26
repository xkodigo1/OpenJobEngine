using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Infrastructure.Options;

namespace OpenJobEngine.Infrastructure.Providers.Computrabajo;

public sealed class ComputrabajoProvider(
    HttpClient httpClient,
    ComputrabajoHtmlParser parser,
    IPageContentFetcher pageContentFetcher,
    ComputrabajoProviderOptions options) : IJobProvider
{
    public string SourceName => options.SourceName;

    public async Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
    {
        var jobs = new List<RawJobOffer>();

        for (var page = 1; page <= Math.Max(1, options.MaxPages); page++)
        {
            var url = BuildSearchUrl(page);
            var html = options.UsePlaywright
                ? await pageContentFetcher.GetHtmlAsync(url, cancellationToken)
                : await httpClient.GetStringAsync(url, cancellationToken);

            var pageJobs = parser.Parse(SourceName, html, options.BaseUrl);
            if (pageJobs.Count == 0)
            {
                break;
            }

            jobs.AddRange(pageJobs);

            if (options.DelayMs > 0 && page < options.MaxPages)
            {
                await Task.Delay(options.DelayMs, cancellationToken);
            }
        }

        return jobs
            .GroupBy(x => x.SourceJobId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
    }

    private string BuildSearchUrl(int page)
    {
        var query = options.SearchTerm.Trim().Replace(' ', '-').ToLowerInvariant();
        return $"{options.BaseUrl.TrimEnd('/')}/trabajo-de-{Uri.EscapeDataString(query)}?p={page}";
    }
}
