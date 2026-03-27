using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text;
using OpenJobEngine.Infrastructure.Options;
using OpenJobEngine.Infrastructure.Providers.Lever;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class LeverJobProviderTests
{
    [Fact]
    public async Task CollectAsync_PaginatesAndMapsLeverPostings()
    {
        var responses = new Dictionary<int, string>
        {
            [0] = """
            [
              {
                "id": "job-1",
                "text": "Senior Backend Engineer",
                "categories": {
                  "location": "Bogota, Colombia",
                  "commitment": "full-time",
                  "team": "Engineering",
                  "department": "Platform",
                  "allLocations": [{ "text": "Bogota, Colombia" }]
                },
                "openingPlain": "Build APIs in C#.",
                "descriptionBodyPlain": "Own cloud services.",
                "additionalPlain": "Remote-friendly.",
                "hostedUrl": "https://jobs.lever.co/acme/job-1",
                "applyUrl": "https://jobs.lever.co/acme/job-1/apply",
                "country": "CO",
                "workplaceType": "hybrid",
                "salaryRange": {
                  "currency": "USD",
                  "interval": "yearly",
                  "min": 9000,
                  "max": 12000
                },
                "salaryDescriptionPlain": "Negotiable"
              }
            ]
            """,
            [1] = """
            [
              {
                "id": "job-2",
                "text": "Platform Engineer",
                "categories": {
                  "location": "Remote",
                  "commitment": "full-time",
                  "team": "Platform",
                  "department": "Engineering",
                  "allLocations": []
                },
                "descriptionPlain": "Kubernetes and observability.",
                "hostedUrl": "https://jobs.lever.co/acme/job-2",
                "country": "US",
                "workplaceType": "remote"
              }
            ]
            """,
            [2] = "[]"
        };

        var handler = new RecordingHandler(request =>
        {
            var query = QueryHelpers.ParseQuery(request.RequestUri!.Query);
            var skip = int.Parse(query["skip"]!.ToString());
            var body = responses[skip];
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var client = new HttpClient(handler);
        var provider = new LeverJobProvider(client, new LeverProviderOptions
        {
            Enabled = true,
            Site = "acme",
            CompanyName = "Acme",
            PageSize = 1,
            MaxPages = 3
        });

        var jobs = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("job-1", jobs.First().SourceJobId);
        Assert.Equal("Senior Backend Engineer", jobs.First().Title);
        Assert.Equal("Acme", jobs.First().CompanyName);
        Assert.Equal("Bogota, Colombia", jobs.First().LocationText);
        Assert.Contains("USD 9000 - 12000", jobs.First().SalaryText);
        Assert.Contains("Negotiable", jobs.First().SalaryText);
        Assert.Contains("Build APIs in C#", jobs.First().Description);
        Assert.Equal("https://jobs.lever.co/acme/job-1", jobs.First().Url);
        Assert.Equal("job-2", jobs.Last().SourceJobId);
        Assert.Equal(3, handler.RequestedUris.Count);
        Assert.Contains(handler.RequestedUris, uri => uri.Contains("skip=0") && uri.Contains("limit=1") && uri.Contains("mode=json"));
        Assert.Contains(handler.RequestedUris, uri => uri.Contains("skip=1") && uri.Contains("limit=1") && uri.Contains("mode=json"));
        Assert.Contains(handler.RequestedUris, uri => uri.Contains("skip=2") && uri.Contains("limit=1") && uri.Contains("mode=json"));
    }

    [Fact]
    public async Task CollectAsync_RequiresSite()
    {
        using var client = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        }));

        var provider = new LeverJobProvider(client, new LeverProviderOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CollectAsync(CancellationToken.None));
        Assert.Contains("Site", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<string> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri!.ToString());
            return Task.FromResult(responseFactory(request));
        }
    }
}
