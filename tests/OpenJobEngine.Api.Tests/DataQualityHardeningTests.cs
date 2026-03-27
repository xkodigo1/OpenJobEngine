using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Catalog;
using OpenJobEngine.Infrastructure.Persistence;
using OpenJobEngine.Infrastructure.Services;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class DataQualityHardeningTests
{
    [Fact]
    public void JobEnrichment_NormalizesLatamSalarySignals_AndFlagsInference()
    {
        var service = new DefaultJobEnrichmentService(new JsonTechnologyTaxonomyProvider());

        var copJob = CreateJobOffer(
            title: "Backend Engineer",
            description: "Salario competitivo para backend engineer.",
            locationText: "Bogota, Colombia",
            salaryText: "$12 millones mensuales",
            sourceName: "manual",
            sourceJobId: "cop-1");
        var copRaw = new RawJobOffer(
            "manual",
            "cop-1",
            copJob.Title,
            copJob.CompanyName,
            copJob.Description,
            copJob.LocationText,
            copJob.SalaryText,
            copJob.Url,
            copJob.PublishedAtUtc,
            new Dictionary<string, string>());

        service.Enrich(copJob, copRaw);

        Assert.Equal(12_000_000m, copJob.SalaryMin);
        Assert.Equal(12_000_000m, copJob.SalaryMax);
        Assert.Equal("COP", copJob.SalaryCurrency);
        Assert.Contains("salary_currency_inferred", copJob.GetQualityFlags(), StringComparer.OrdinalIgnoreCase);

        var usdJob = CreateJobOffer(
            title: "Senior Backend Engineer",
            description: "Compensation package in USD for remote LATAM candidates.",
            locationText: "Remote LATAM",
            salaryText: "USD 6k - 8k monthly",
            sourceName: "manual",
            sourceJobId: "usd-1");
        var usdRaw = new RawJobOffer(
            "manual",
            "usd-1",
            usdJob.Title,
            usdJob.CompanyName,
            usdJob.Description,
            usdJob.LocationText,
            usdJob.SalaryText,
            usdJob.Url,
            usdJob.PublishedAtUtc,
            new Dictionary<string, string>());

        service.Enrich(usdJob, usdRaw);

        Assert.Equal(6_000m, usdJob.SalaryMin);
        Assert.Equal(8_000m, usdJob.SalaryMax);
        Assert.Equal("USD", usdJob.SalaryCurrency);
        Assert.DoesNotContain("salary_currency_inferred", usdJob.GetQualityFlags(), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CollectionRun_WhenProviderFails_DeactivatesStaleObservations()
    {
        await using var factory = new ApiWebApplicationFactory(services =>
        {
            services.RemoveAll<IJobProvider>();
            services.AddSingleton<IJobProvider>(new ThrowingProvider("stale-provider"));
        });

        using var client = factory.CreateClient();
        var jobId = Guid.NewGuid();
        await SeedStaleObservationAsync(factory.Services, jobId, "stale-provider", "stale-job-1");

        var response = await client.PostAsync("/api/collections/run/stale-provider", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var sourceSummary = payload.GetProperty("sources")[0];
        Assert.False(sourceSummary.GetProperty("success").GetBoolean());
        Assert.Equal(1, sourceSummary.GetProperty("deactivatedJobs").GetInt32());
        Assert.Equal(1, sourceSummary.GetProperty("staleDeactivatedJobs").GetInt32());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenJobEngineDbContext>();
        var job = await dbContext.JobOffers.FindAsync(jobId);
        var observation = dbContext.JobOfferSourceObservations.Single(x => x.JobOfferId == jobId);
        var history = dbContext.JobOfferHistoryEntries
            .Where(x => x.JobOfferId == jobId)
            .ToList()
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToArray();

        Assert.NotNull(job);
        Assert.False(job!.IsActive);
        Assert.False(observation.IsActive);
        Assert.Contains(history, entry => entry.EventType == JobOfferHistoryEventType.Deactivated);
    }

    private static JobOffer CreateJobOffer(
        string title,
        string description,
        string locationText,
        string salaryText,
        string sourceName,
        string sourceJobId)
    {
        var now = DateTimeOffset.UtcNow;
        return new JobOffer(
            Guid.NewGuid(),
            title,
            "Acme",
            description,
            locationText,
            EmploymentType.FullTime,
            SeniorityLevel.Unknown,
            WorkMode.Unknown,
            null,
            null,
            null,
            null,
            salaryText,
            null,
            null,
            null,
            false,
            $"https://jobs.example.test/{sourceJobId}",
            sourceName,
            sourceJobId,
            now.AddDays(-1),
            now,
            now,
            sourceJobId,
            true,
            0.15m,
            null);
    }

    private static async Task SeedStaleObservationAsync(IServiceProvider services, Guid jobId, string sourceName, string sourceJobId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenJobEngineDbContext>();
        var seenAt = DateTimeOffset.UtcNow.AddDays(-14);

        var job = new JobOffer(
            jobId,
            "Legacy Backend Engineer",
            "Stale Corp",
            "Old provider record that should be deactivated after staleness.",
            "Bogota, Colombia",
            EmploymentType.FullTime,
            SeniorityLevel.Senior,
            WorkMode.Remote,
            "Bogota",
            "Cundinamarca",
            "CO",
            "America/Bogota",
            "COP 12000000",
            12_000_000m,
            12_000_000m,
            "COP",
            true,
            "https://jobs.example.test/stale",
            sourceName,
            sourceJobId,
            seenAt.AddDays(-1),
            seenAt,
            seenAt,
            "stale-key",
            true,
            0.82m,
            null);

        var observation = new JobOfferSourceObservation(
            Guid.NewGuid(),
            jobId,
            sourceName,
            sourceJobId,
            true,
            seenAt,
            seenAt,
            "stale-hash");

        dbContext.JobOffers.Add(job);
        dbContext.JobOfferSourceObservations.Add(observation);
        await dbContext.SaveChangesAsync();
    }

    private sealed class ThrowingProvider(string sourceName) : IJobProvider
    {
        public string SourceName { get; } = sourceName;

        public Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider outage");
        }
    }
}
