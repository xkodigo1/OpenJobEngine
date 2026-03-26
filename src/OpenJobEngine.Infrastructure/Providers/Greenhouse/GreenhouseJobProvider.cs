using System.Net.Http.Json;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Infrastructure.Options;

namespace OpenJobEngine.Infrastructure.Providers.Greenhouse;

public sealed class GreenhouseJobProvider(HttpClient httpClient, GreenhouseProviderOptions options) : IJobProvider
{
    public string SourceName => options.SourceName;

    public async Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.BoardToken))
        {
            throw new InvalidOperationException("Greenhouse provider requires BoardToken.");
        }

        var response = await httpClient.GetFromJsonAsync<GreenhouseJobsResponse>(BuildUri(), cancellationToken);
        var jobs = response?.Jobs ?? [];

        return jobs
            .Select(x => GreenhouseJobMapper.Map(SourceName, x))
            .GroupBy(x => x.SourceJobId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
    }

    private string BuildUri()
    {
        var query = options.IncludeContent ? "?content=true" : string.Empty;
        return $"{options.BaseUrl.TrimEnd('/')}/{options.BoardToken.Trim()}/jobs{query}";
    }
}
