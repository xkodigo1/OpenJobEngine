using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Persistence;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class AlertDispatchAndMetricsTests
{
    [Fact]
    public async Task DispatchEndpoint_RecordsAlertDeliveries_AndMetricsExposeOperationalData()
    {
        var fakeWebhookService = new FakeAlertWebhookPublisher();
        await using var factory = new ApiWebApplicationFactory(services =>
        {
            services.RemoveAll<IAlertWebhookPublisher>();
            services.AddSingleton<IAlertWebhookPublisher>(fakeWebhookService);
        });

        using var client = factory.CreateClient();
        var jobId = Guid.NewGuid();
        await SeedOperationalDataAsync(factory.Services, jobId);

        var profileResponse = await client.PostAsJsonAsync("/api/profiles", new
        {
            targetTitle = "Senior Backend Engineer",
            professionalSummary = "Backend engineer focused on .NET and cloud services.",
            yearsOfExperience = 7,
            seniorityLevel = "Senior",
            preferredWorkMode = "Remote",
            acceptRemote = true,
            acceptHybrid = true,
            acceptOnSite = false,
            salaryMin = 8000,
            salaryMax = 12000,
            salaryCurrency = "USD",
            currentCity = "Bogota",
            currentRegion = "Cundinamarca",
            currentCountryCode = "CO",
            targetCities = Array.Empty<string>(),
            targetCountries = new[] { "CO" },
            targetTimezones = new[] { "America/Bogota" },
            excludedWorkModes = Array.Empty<string>(),
            includedCompanyKeywords = new[] { "Cloud" },
            excludedCompanyKeywords = Array.Empty<string>(),
            isWillingToRelocate = false,
            skills = new object[]
            {
                new { skillName = "C#", skillSlug = "csharp", category = "Language", yearsExperience = 7, proficiencyScore = 5 },
                new { skillName = "ASP.NET Core", skillSlug = "aspnet-core", category = "Framework", yearsExperience = 6, proficiencyScore = 5 }
            },
            languages = new object[]
            {
                new { languageCode = "en", languageName = "English", proficiency = "B2" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
        var profileJson = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = profileJson.GetProperty("id").GetGuid();

        var alertResponse = await client.PostAsJsonAsync($"/api/profiles/{profileId:D}/alerts", new
        {
            name = "Relevant webhook alert",
            channelType = "Webhook",
            target = "https://example.test/webhooks/openjobengine",
            minimumMatchScore = 70,
            minimumNewMatchScore = 75,
            onlyNewJobs = true,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, alertResponse.StatusCode);

        var matchingResponse = await client.GetAsync($"/api/profiles/{profileId:D}/matches");
        Assert.Equal(HttpStatusCode.OK, matchingResponse.StatusCode);

        var dispatchResponse = await client.PostAsync("/api/alerts/dispatch", content: null);
        Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);

        var dispatchPayload = await dispatchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, dispatchPayload.GetProperty("evaluatedAlerts").GetInt32());
        Assert.Equal(1, dispatchPayload.GetProperty("matchedJobs").GetInt32());
        Assert.Equal(1, dispatchPayload.GetProperty("deliveredCount").GetInt32());
        Assert.Equal(1, fakeWebhookService.InvocationCount);

        var alertMetricsResponse = await client.GetAsync("/api/metrics/alerts");
        Assert.Equal(HttpStatusCode.OK, alertMetricsResponse.StatusCode);
        var alertMetrics = await alertMetricsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, alertMetrics.GetProperty("activeAlerts").GetInt32());
        Assert.True(alertMetrics.GetProperty("deliveriesLast7Days").GetInt64() >= 1);
        Assert.Single(alertMetrics.GetProperty("recentDeliveries").EnumerateArray());
        Assert.Equal(jobId, alertMetrics.GetProperty("recentDeliveries")[0].GetProperty("jobId").GetGuid());

        var matchingMetricsResponse = await client.GetAsync("/api/metrics/matching");
        Assert.Equal(HttpStatusCode.OK, matchingMetricsResponse.StatusCode);
        var matchingMetrics = await matchingMetricsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(matchingMetrics.GetProperty("executionsLast7Days").GetInt32() >= 1);
        Assert.Equal("2026.03.v1", matchingMetrics.GetProperty("ruleVersion").GetString());

        var providerOpsResponse = await client.GetAsync("/api/metrics/providers/operations");
        Assert.Equal(HttpStatusCode.OK, providerOpsResponse.StatusCode);
        var providerOps = await providerOpsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sources = providerOps.GetProperty("sources").EnumerateArray().ToArray();
        Assert.Single(sources);
        Assert.Equal("lever", sources[0].GetProperty("sourceName").GetString());
        Assert.Equal("Completed", sources[0].GetProperty("lastStatus").GetString());
    }

    private static async Task SeedOperationalDataAsync(IServiceProvider services, Guid jobId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenJobEngineDbContext>();
        var now = DateTimeOffset.UtcNow;

        var source = new JobSource(Guid.NewGuid(), "lever", "provider", true, "Lever board");
        var execution = ScrapeExecution.Start("lever", now.AddMinutes(-20));
        execution.Complete(now.AddMinutes(-10), 12, 4, 3, 1, 0);

        var job = new JobOffer(
            jobId,
            "Senior Backend Engineer",
            "Acme Cloud",
            "Build .NET APIs with C# and ASP.NET Core.",
            "Bogota, Colombia",
            EmploymentType.FullTime,
            SeniorityLevel.Senior,
            WorkMode.Remote,
            "Bogota",
            "Cundinamarca",
            "CO",
            "America/Bogota",
            "USD 9000 - 11000",
            9000,
            11000,
            "USD",
            true,
            "https://jobs.example.com/acme/backend",
            "lever",
            "lever-job-1",
            now.AddMinutes(-15),
            now.AddMinutes(-15),
            now.AddMinutes(-15),
            "lever-job-1",
            true,
            0.95m,
            null);

        job.ReplaceSkillTags(new[]
        {
            new JobOfferSkillTag(Guid.NewGuid(), jobId, "C#", "csharp", SkillCategory.Language, true, 0.98m),
            new JobOfferSkillTag(Guid.NewGuid(), jobId, "ASP.NET Core", "aspnet-core", SkillCategory.Framework, false, 0.94m)
        });
        job.ReplaceLanguageRequirements(new[]
        {
            new JobOfferLanguageRequirement(Guid.NewGuid(), jobId, "en", "English", LanguageProficiency.B1, true, 0.90m)
        });

        dbContext.JobSources.Add(source);
        dbContext.ScrapeExecutions.Add(execution);
        dbContext.JobOffers.Add(job);
        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeAlertWebhookPublisher : IAlertWebhookPublisher
    {
        public int InvocationCount { get; private set; }

        public Task<AlertWebhookDispatchResultDto> SendAsync(
            AlertWebhookPayloadDto payload,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(new AlertWebhookDispatchResultDto(true, 202, "{\"ok\":true}", null));
        }
    }
}
