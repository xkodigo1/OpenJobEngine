using System.Net.Http.Json;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Infrastructure.Options;

namespace OpenJobEngine.Infrastructure.Providers.Lever;

public sealed class LeverJobProvider(HttpClient httpClient, LeverProviderOptions options) : IJobProvider
{
    public string SourceName => options.SourceName;

    public async Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Site))
        {
            throw new InvalidOperationException("Lever provider requires Site.");
        }

        var jobs = new List<RawJobOffer>();
        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var maxPages = Math.Max(1, options.MaxPages);

        for (var page = 0; page < maxPages; page++)
        {
            var response = await httpClient.GetFromJsonAsync<LeverJobPostingsResponse[]>(
                BuildUri(page * pageSize, pageSize),
                cancellationToken);

            var items = response ?? [];
            if (items.Length == 0)
            {
                break;
            }

            jobs.AddRange(items.Select(job => LeverJobMapper.Map(SourceName, options.Site.Trim(), options.CompanyName, job, options.BaseUrl)));

            if (items.Length < pageSize)
            {
                break;
            }
        }

        return jobs
            .GroupBy(x => x.SourceJobId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
    }

    private string BuildUri(int skip, int limit)
    {
        var query = new Dictionary<string, string?>
        {
            ["mode"] = "json",
            ["skip"] = skip.ToString(),
            ["limit"] = limit.ToString()
        };

        var queryString = string.Join(
            "&",
            query
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return $"{options.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(options.Site.Trim())}?{queryString}";
    }
}
