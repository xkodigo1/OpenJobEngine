using System.Net.Http.Json;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Infrastructure.Options;

namespace OpenJobEngine.Infrastructure.Providers.Adzuna;

public sealed class AdzunaJobProvider(HttpClient httpClient, AdzunaProviderOptions options) : IJobProvider
{
    public string SourceName => options.SourceName;

    public async Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.AppId) || string.IsNullOrWhiteSpace(options.AppKey))
        {
            throw new InvalidOperationException("Adzuna provider requires AppId and AppKey.");
        }

        var uri = BuildUri();
        var response = await httpClient.GetFromJsonAsync<AdzunaSearchResponse>(uri, cancellationToken);
        var results = response?.Results ?? new List<AdzunaJobContract>();

        return results.Select(x => AdzunaJobMapper.Map(SourceName, x)).ToArray();
    }

    private string BuildUri()
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var query = new Dictionary<string, string?>
        {
            ["app_id"] = options.AppId,
            ["app_key"] = options.AppKey,
            ["results_per_page"] = options.ResultsPerPage.ToString(),
            ["what"] = options.SearchTerm,
            ["where"] = options.Location,
            ["content-type"] = "application/json"
        };

        var queryString = string.Join(
            "&",
            query
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return $"{baseUrl}/{options.CountryCode}/search/{options.Page}?{queryString}";
    }
}
