using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Persistence;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class MatchingEnhancementsTests
{
    [Fact]
    public async Task NewHighPriorityEndpoint_ReturnsOnlyRelevantMatches_AndJobMatchIncludesHardFailures()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var preferredJobId = Guid.NewGuid();
        var excludedJobId = Guid.NewGuid();
        await SeedJobsAsync(factory.Services, preferredJobId, excludedJobId);

        var profileResponse = await client.PostAsJsonAsync("/api/profiles", new
        {
            targetTitle = "Senior Backend Engineer",
            professionalSummary = "Backend engineer focused on .NET and cloud platforms.",
            yearsOfExperience = 6,
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
            excludedWorkModes = new[] { "OnSite" },
            includedCompanyKeywords = new[] { "Cloud" },
            excludedCompanyKeywords = new[] { "Outsourcing" },
            isWillingToRelocate = false,
            skills = new object[]
            {
                new { skillName = "C#", skillSlug = "csharp", category = "Language", yearsExperience = 6, proficiencyScore = 5 },
                new { skillName = "ASP.NET Core", skillSlug = "aspnet-core", category = "Framework", yearsExperience = 5, proficiencyScore = 5 }
            },
            languages = new object[]
            {
                new { languageCode = "en", languageName = "English", proficiency = "B2" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = profile.GetProperty("id").GetGuid();

        var newHighPriorityResponse = await client.GetAsync($"/api/profiles/{profileId:D}/matches/new-high-priority");
        Assert.Equal(HttpStatusCode.OK, newHighPriorityResponse.StatusCode);

        var newHighPriorityPayload = await newHighPriorityResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newHighPriorityItems = newHighPriorityPayload.GetProperty("results").GetProperty("items").EnumerateArray().ToArray();

        Assert.Single(newHighPriorityItems);
        Assert.Equal(preferredJobId, newHighPriorityItems[0].GetProperty("job").GetProperty("id").GetGuid());
        Assert.NotEmpty(newHighPriorityItems[0].GetProperty("strongMatches").EnumerateArray());
        Assert.Empty(newHighPriorityItems[0].GetProperty("hardFailures").EnumerateArray());

        var excludedJobMatchResponse = await client.GetAsync($"/api/jobs/{excludedJobId:D}/match?profileId={profileId:D}");
        Assert.Equal(HttpStatusCode.OK, excludedJobMatchResponse.StatusCode);

        var excludedJobMatch = await excludedJobMatchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(excludedJobMatch.GetProperty("hardFailures").EnumerateArray());
    }

    private static async Task SeedJobsAsync(IServiceProvider services, Guid preferredJobId, Guid excludedJobId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenJobEngineDbContext>();

        var now = DateTimeOffset.UtcNow;

        var preferredJob = new JobOffer(
            preferredJobId,
            "Senior Backend Engineer",
            "Acme Cloud",
            "Build cloud APIs with C# and ASP.NET Core.",
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
            "https://example.com/jobs/preferred",
            "manual-test",
            "preferred-job",
            now.AddMinutes(-5),
            now.AddMinutes(-5),
            now.AddMinutes(-5),
            "preferred-job",
            true,
            0.95m,
            null);
        preferredJob.ReplaceSkillTags(new[]
        {
            new JobOfferSkillTag(Guid.NewGuid(), preferredJobId, "C#", "csharp", SkillCategory.Language, true, 0.95m),
            new JobOfferSkillTag(Guid.NewGuid(), preferredJobId, "ASP.NET Core", "aspnet-core", SkillCategory.Framework, false, 0.90m)
        });
        preferredJob.ReplaceLanguageRequirements(new[]
        {
            new JobOfferLanguageRequirement(Guid.NewGuid(), preferredJobId, "en", "English", LanguageProficiency.B1, true, 0.90m)
        });

        var excludedJob = new JobOffer(
            excludedJobId,
            "Backend Support Engineer",
            "Legacy Outsourcing",
            "On-site support role for legacy backend systems.",
            "Madrid, Spain",
            EmploymentType.FullTime,
            SeniorityLevel.Mid,
            WorkMode.OnSite,
            "Madrid",
            "Madrid",
            "ES",
            "Europe/Madrid",
            "USD 3500 - 4200",
            3500,
            4200,
            "USD",
            false,
            "https://example.com/jobs/excluded",
            "manual-test",
            "excluded-job",
            now.AddMinutes(-2),
            now.AddMinutes(-2),
            now.AddMinutes(-2),
            "excluded-job",
            true,
            0.90m,
            null);
        excludedJob.ReplaceSkillTags(new[]
        {
            new JobOfferSkillTag(Guid.NewGuid(), excludedJobId, "Java", "java", SkillCategory.Language, true, 0.92m)
        });

        dbContext.JobOffers.Add(preferredJob);
        dbContext.JobOffers.Add(excludedJob);
        await dbContext.SaveChangesAsync();
    }
}
