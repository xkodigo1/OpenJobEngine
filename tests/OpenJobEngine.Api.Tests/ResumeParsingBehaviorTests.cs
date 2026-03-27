using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;
using OpenJobEngine.Infrastructure.Catalog;
using OpenJobEngine.Infrastructure.Resume;
using OpenJobEngine.Domain.Enums;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class ResumeParsingBehaviorTests
{
    [Fact]
    public void Extractor_UsesSectionSegmentation_AndDetectsSpanishEnglishSignals()
    {
        var extractor = new HeuristicResumeProfileExtractor(new FakeTaxonomyProvider());
        var result = extractor.Extract("""
            Juan Perez
            Bogota, Colombia

            Professional Summary
            Senior backend engineer with 7+ years of experience building APIs and distributed systems.

            Experience
            Senior Backend Engineer - Acme Cloud
            Led C# and ASP.NET Core services since 2017.
            Worked with PostgreSQL, Azure and Docker.

            Skills
            C#, ASP.NET Core, PostgreSQL, Azure, Docker

            Languages
            English - B2
            Spanish - Native
            """);

        Assert.Equal("Backend Developer", result.SuggestedProfile.TargetTitle);
        Assert.Equal("Senior", result.SuggestedProfile.SeniorityLevel);
        Assert.Equal("Bogota", result.SuggestedProfile.CurrentCity);
        Assert.Equal("Cundinamarca", result.SuggestedProfile.CurrentRegion);
        Assert.Equal("CO", result.SuggestedProfile.CurrentCountryCode);
        Assert.Equal("Unknown", result.SuggestedProfile.PreferredWorkMode);
        Assert.False(result.SuggestedProfile.AcceptRemote);
        Assert.False(result.SuggestedProfile.AcceptHybrid);
        Assert.False(result.SuggestedProfile.AcceptOnSite);
        Assert.Empty(result.SuggestedProfile.TargetCities);
        Assert.Empty(result.SuggestedProfile.TargetCountries);
        Assert.Empty(result.SuggestedProfile.TargetTimezones);
        Assert.Equal(7m, result.EstimatedYearsOfExperience);
        Assert.NotNull(result.SuggestedProfile.ProfessionalSummary);
        Assert.Contains("Senior backend engineer", result.SuggestedProfile.ProfessionalSummary!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("summary", result.DetectedSections.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("experience", result.DetectedSections.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("skills", result.DetectedSections.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("languages", result.DetectedSections.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.DetectedSkills, skill => string.Equals(skill, "C#", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.DetectedSkills, skill => string.Equals(skill, "ASP.NET Core", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.DetectedSkills, skill => string.Equals(skill, "PostgreSQL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.DetectedLanguages, language => string.Equals(language, "English", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.DetectedLanguages, language => string.Equals(language, "Spanish", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("B2", result.SuggestedProfile.Languages.Single(x => x.LanguageCode == "en").Proficiency);
        Assert.Equal("Native", result.SuggestedProfile.Languages.Single(x => x.LanguageCode == "es").Proficiency);
        Assert.True(result.FieldConfidences["yearsOfExperience"] > 0.8m);
        Assert.True(result.FieldConfidences["languages"] > 0.8m);
        Assert.True(result.FieldConfidences["location"] > 0.7m);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Extractor_WhenSignalsAreSparse_DoesNotInventSummaryOrLocation()
    {
        var extractor = new HeuristicResumeProfileExtractor(new FakeTaxonomyProvider());
        var result = extractor.Extract("""
            Ana Lopez
            Software developer
            C#
            """);

        Assert.Equal("Software Engineer", result.SuggestedProfile.TargetTitle);
        Assert.Equal("Unknown", result.SuggestedProfile.SeniorityLevel);
        Assert.Equal("Unknown", result.SuggestedProfile.PreferredWorkMode);
        Assert.False(result.SuggestedProfile.AcceptRemote);
        Assert.False(result.SuggestedProfile.AcceptHybrid);
        Assert.False(result.SuggestedProfile.AcceptOnSite);
        Assert.Null(result.SuggestedProfile.ProfessionalSummary);
        Assert.Null(result.EstimatedYearsOfExperience);
        Assert.Null(result.SuggestedProfile.CurrentCity);
        Assert.Null(result.SuggestedProfile.CurrentCountryCode);
        Assert.Empty(result.SuggestedProfile.TargetCities);
        Assert.Empty(result.SuggestedProfile.TargetCountries);
        Assert.Empty(result.SuggestedProfile.TargetTimezones);
        Assert.Single(result.DetectedSkills);
        Assert.Empty(result.DetectedLanguages);
        Assert.Contains(result.Warnings, warning => warning.Contains("experiencia total", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, warning => warning.Contains("ciudad actual", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, warning => warning.Contains("resumen profesional", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, warning => warning.Contains("idiomas", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0.0m, result.FieldConfidences["professionalSummary"]);
        Assert.Equal(0.0m, result.FieldConfidences["location"]);
        Assert.Equal(0.0m, result.FieldConfidences["yearsOfExperience"]);
    }

    [Fact]
    public async Task ResumeImport_ApplyToProfileFalse_ReturnsPreviewAndDoesNotMutateProfile()
    {
        var resumeText = """
            Juan Perez
            Bogota, Colombia

            Professional Summary
            Senior backend engineer with 7+ years of experience building APIs and distributed systems.

            Experience
            Senior Backend Engineer - Acme Cloud
            Led C# and ASP.NET Core services since 2017.
            Worked with PostgreSQL, Azure and Docker.

            Skills
            C#, ASP.NET Core, PostgreSQL, Azure, Docker

            Languages
            English - B2
            Spanish - Native
            """;

        var fakeTextExtractor = new StaticResumeTextExtractor(resumeText);
        await using var factory = new ApiWebApplicationFactory(services =>
        {
            services.RemoveAll<IResumeTextExtractor>();
            services.AddSingleton<IResumeTextExtractor>(fakeTextExtractor);
        });

        using var client = factory.CreateClient();
        var profileId = await CreateProfileAsync(client);

        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        multipart.Add(fileContent, "file", "resume.pdf");
        multipart.Add(new StringContent("false"), "applyToProfile");

        var response = await client.PostAsync($"/api/profiles/{profileId:D}/resume", multipart);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = await response.Content.ReadFromJsonAsync<ResumeImportPreviewDto>();
        Assert.NotNull(preview);
        Assert.False(preview!.AppliedToProfile);
        Assert.Equal("Backend Developer", preview.SuggestedImport.SuggestedProfile.TargetTitle);
        Assert.True(preview.SuggestedImport.FieldConfidences["yearsOfExperience"] > 0.8m);
        Assert.Empty(preview.Warnings);

        var storedProfileResponse = await client.GetAsync($"/api/profiles/{profileId:D}");
        Assert.Equal(HttpStatusCode.OK, storedProfileResponse.StatusCode);

        var storedProfile = await storedProfileResponse.Content.ReadFromJsonAsync<CandidateProfileDto>();
        Assert.NotNull(storedProfile);
        Assert.Equal(1m, storedProfile!.YearsOfExperience);
        Assert.Equal("Software Engineer", storedProfile.TargetTitle);
        Assert.Equal("Junior", storedProfile.SeniorityLevel);
        Assert.Null(storedProfile.CurrentCity);
        Assert.Null(storedProfile.CurrentCountryCode);
        Assert.Equal("Unknown", storedProfile.PreferredWorkMode);
        Assert.False(storedProfile.AcceptRemote);
        Assert.Empty(storedProfile.TargetCities);
        Assert.Single(storedProfile.Skills);
        Assert.Empty(storedProfile.Languages);
    }

    private static async Task<Guid> CreateProfileAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/profiles", new
        {
            targetTitle = "Software Engineer",
            professionalSummary = "Initial profile",
            yearsOfExperience = 1,
            seniorityLevel = "Junior",
            preferredWorkMode = "Unknown",
            acceptRemote = false,
            acceptHybrid = false,
            acceptOnSite = false,
            salaryMin = (decimal?)null,
            salaryMax = (decimal?)null,
            salaryCurrency = (string?)null,
            currentCity = (string?)null,
            currentRegion = (string?)null,
            currentCountryCode = (string?)null,
            targetCities = Array.Empty<string>(),
            targetCountries = Array.Empty<string>(),
            targetTimezones = Array.Empty<string>(),
            excludedWorkModes = Array.Empty<string>(),
            includedCompanyKeywords = Array.Empty<string>(),
            excludedCompanyKeywords = Array.Empty<string>(),
            isWillingToRelocate = false,
            skills = new object[] { new { skillName = "C#", skillSlug = "csharp", category = "Language", yearsExperience = 1, proficiencyScore = 3 } },
            languages = Array.Empty<object>()
        });

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<CandidateProfileDto>();
        return profile!.Id;
    }

    private sealed class StaticResumeTextExtractor(string text) : IResumeTextExtractor
    {
        public Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken)
        {
            return Task.FromResult(text);
        }
    }

    private sealed class FakeTaxonomyProvider : ITechnologyTaxonomyProvider
    {
        private static readonly IReadOnlyCollection<CatalogSkillDefinition> Skills =
        [
            new("C#", "csharp", SkillCategory.Language, ["c#", ".net", "dotnet"], ["csharp"], [], []),
            new("ASP.NET Core", "aspnet-core", SkillCategory.Framework, ["asp.net core", "web api"], ["asp.net"], [], []),
            new("PostgreSQL", "postgresql", SkillCategory.Database, ["postgresql", "postgres"], ["postgres"], [], []),
            new("Azure", "azure", SkillCategory.Cloud, ["azure"], [], [], []),
            new("Docker", "docker", SkillCategory.Tool, ["docker"], [], [], [])
        ];

        private static readonly IReadOnlyCollection<CatalogLanguageDefinition> Languages =
        [
            new("en", "English", ["english", "ingles"]),
            new("es", "Spanish", ["spanish", "espanol"])
        ];

        private static readonly IReadOnlyCollection<CatalogLocationDefinition> Locations =
        [
            new("bogota", "Bogota", "Cundinamarca", "CO", "America/Bogota", ["bogota", "bogota, colombia"])
        ];

        public IReadOnlyCollection<CatalogSkillDefinition> GetSkills() => Skills;

        public IReadOnlyCollection<CatalogLanguageDefinition> GetLanguages() => Languages;

        public IReadOnlyCollection<CatalogLocationDefinition> GetLocations() => Locations;
    }
}
