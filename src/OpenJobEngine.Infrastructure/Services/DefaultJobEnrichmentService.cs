using System.Globalization;
using System.Text.RegularExpressions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Catalog;

namespace OpenJobEngine.Infrastructure.Services;

public sealed partial class DefaultJobEnrichmentService : IJobEnrichmentService
{
    private readonly IReadOnlyCollection<CatalogSkillDefinition> skills;
    private readonly IReadOnlyCollection<CatalogLanguageDefinition> languages;
    private readonly IReadOnlyCollection<CatalogLocationDefinition> locations;

    public DefaultJobEnrichmentService(ITechnologyTaxonomyProvider taxonomyProvider)
    {
        skills = taxonomyProvider.GetSkills();
        languages = taxonomyProvider.GetLanguages();
        locations = taxonomyProvider.GetLocations();
    }

    public JobOffer Enrich(JobOffer jobOffer, RawJobOffer rawJobOffer)
    {
        var corpus = $"{jobOffer.Title} {jobOffer.Description} {jobOffer.LocationText}".ToLowerInvariant();
        var workMode = ResolveWorkMode(corpus, rawJobOffer.Metadata, jobOffer.WorkMode);
        var (city, region, countryCode) = ResolveLocation(jobOffer.LocationText, rawJobOffer.Metadata);
        var skillTags = ExtractSkillTags(jobOffer.Id, corpus);
        var languageRequirements = ExtractLanguageRequirements(jobOffer.Id, corpus);
        var seniorityLevel = ResolveSeniority(corpus, jobOffer.SeniorityLevel);
        var (salaryMin, salaryMax, salaryCurrency) = ResolveSalary(jobOffer, rawJobOffer, corpus);
        var qualityFlags = BuildQualityFlags(jobOffer, skillTags, languageRequirements, workMode, countryCode);
        var qualityScore = CalculateQualityScore(jobOffer, skillTags.Count, languageRequirements.Count, qualityFlags.Count, workMode, countryCode);

        jobOffer.SetSeniorityLevel(seniorityLevel);
        jobOffer.SetLocation(city, region, countryCode, workMode);
        jobOffer.SetSalary(jobOffer.SalaryText, salaryMin, salaryMax, salaryCurrency);
        jobOffer.ReplaceSkillTags(skillTags);
        jobOffer.ReplaceLanguageRequirements(languageRequirements);
        jobOffer.SetQuality(qualityScore, qualityFlags);

        return jobOffer;
    }

    private static WorkMode ResolveWorkMode(string corpus, IReadOnlyDictionary<string, string> metadata, WorkMode current)
    {
        if (current != WorkMode.Unknown)
        {
            return current;
        }

        var workplace = metadata.TryGetValue("workplace", out var value) ? value : null;
        var merged = $"{corpus} {workplace}".ToLowerInvariant();

        if (merged.Contains("hybrid") || merged.Contains("hibrid"))
        {
            return WorkMode.Hybrid;
        }

        if (merged.Contains("remote") || merged.Contains("remoto") || merged.Contains("teletrabajo") || merged.Contains("work from home"))
        {
            return WorkMode.Remote;
        }

        if (merged.Contains("presencial") || merged.Contains("on-site") || merged.Contains("onsite"))
        {
            return WorkMode.OnSite;
        }

        return WorkMode.Unknown;
    }

    private (string? City, string? Region, string? CountryCode) ResolveLocation(string? locationText, IReadOnlyDictionary<string, string> metadata)
    {
        var candidates = new[]
        {
            locationText,
            metadata.TryGetValue("city", out var city) ? city : null,
            metadata.TryGetValue("location", out var metadataLocation) ? metadataLocation : null
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var normalized = TextCanonicalizer.CanonicalizeKeyPart(candidate).Replace("-", " ");
            foreach (var location in locations)
            {
                if (location.Aliases.Any(alias => normalized.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                {
                    return (location.City, location.Region, location.CountryCode);
                }
            }
        }

        return (null, null, null);
    }

    private IReadOnlyCollection<JobOfferSkillTag> ExtractSkillTags(Guid jobId, string corpus)
    {
        var tags = new List<JobOfferSkillTag>();

        foreach (var skill in skills)
        {
            var candidates = skill.Tokens
                .Concat(skill.Aliases)
                .Append(skill.Name.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var matchedTokens = candidates
                .Where(token => corpus.Contains(token, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchedTokens.Length == 0)
            {
                continue;
            }

            var isRequired = candidates.Any(token =>
                corpus.Contains($"required {token}", StringComparison.OrdinalIgnoreCase) ||
                corpus.Contains($"must have {token}", StringComparison.OrdinalIgnoreCase) ||
                corpus.Contains($"requerido {token}", StringComparison.OrdinalIgnoreCase) ||
                corpus.Contains($"experiencia en {token}", StringComparison.OrdinalIgnoreCase) ||
                corpus.Contains($"strong {token}", StringComparison.OrdinalIgnoreCase));

            var confidence = matchedTokens.Any(token => string.Equals(token, skill.Name, StringComparison.OrdinalIgnoreCase))
                ? 0.92m
                : matchedTokens.Any(token => skill.Aliases.Contains(token, StringComparer.OrdinalIgnoreCase)) ? 0.84m : 0.78m;

            tags.Add(new JobOfferSkillTag(Guid.NewGuid(), jobId, skill.Name, skill.Slug, skill.Category, isRequired, confidence));
        }

        return tags
            .GroupBy(x => x.SkillSlug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.ConfidenceScore).First())
            .ToArray();
    }

    private IReadOnlyCollection<JobOfferLanguageRequirement> ExtractLanguageRequirements(Guid jobId, string corpus)
    {
        var requirements = new List<JobOfferLanguageRequirement>();

        foreach (var language in languages)
        {
            if (!language.Tokens.Any(token => corpus.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var proficiency = ResolveLanguageProficiency(corpus);
            var isRequired = corpus.Contains("required", StringComparison.OrdinalIgnoreCase)
                || corpus.Contains("requerido", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.Code, "en", StringComparison.OrdinalIgnoreCase);

            requirements.Add(new JobOfferLanguageRequirement(
                Guid.NewGuid(),
                jobId,
                language.Code,
                language.Name,
                proficiency,
                isRequired,
                string.Equals(language.Code, "en", StringComparison.OrdinalIgnoreCase) ? 0.88m : 0.75m));
        }

        if (requirements.Count == 0 && (corpus.Contains("bilingual") || corpus.Contains("bilingue") || corpus.Contains("bilingual english")))
        {
            requirements.Add(new JobOfferLanguageRequirement(
                Guid.NewGuid(),
                jobId,
                "en",
                "English",
                LanguageProficiency.B2,
                true,
                0.65m));
        }

        return requirements
            .GroupBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.ConfidenceScore).First())
            .ToArray();
    }

    private static SeniorityLevel ResolveSeniority(string corpus, SeniorityLevel current)
    {
        if (current != SeniorityLevel.Unknown)
        {
            return current;
        }

        if (corpus.Contains("lead") || corpus.Contains("principal"))
        {
            return SeniorityLevel.Lead;
        }

        if (corpus.Contains("director") || corpus.Contains("head of") || corpus.Contains("chief"))
        {
            return SeniorityLevel.Executive;
        }

        if (corpus.Contains("senior") || corpus.Contains("sr.") || corpus.Contains("ssr"))
        {
            return SeniorityLevel.Senior;
        }

        if (corpus.Contains("semi senior") || corpus.Contains("mid") || corpus.Contains("middle"))
        {
            return SeniorityLevel.Mid;
        }

        if (corpus.Contains("junior") || corpus.Contains("jr.") || corpus.Contains("trainee"))
        {
            return SeniorityLevel.Junior;
        }

        return current;
    }

    private static (decimal? SalaryMin, decimal? SalaryMax, string? Currency) ResolveSalary(JobOffer jobOffer, RawJobOffer rawJobOffer, string corpus)
    {
        if (jobOffer.SalaryMin.HasValue || jobOffer.SalaryMax.HasValue)
        {
            return (jobOffer.SalaryMin, jobOffer.SalaryMax, jobOffer.SalaryCurrency);
        }

        var salaryCorpus = string.Join(" ", new[]
        {
            jobOffer.SalaryText,
            jobOffer.Description,
            rawJobOffer.Metadata.TryGetValue("salary", out var salaryMetadata) ? salaryMetadata : null
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(salaryCorpus))
        {
            return (null, null, null);
        }

        var numbers = SalaryNumberRegex().Matches(salaryCorpus)
            .Select(match => ParseDecimal(match.Value))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        var currency = salaryCorpus.Contains("usd") || salaryCorpus.Contains("us$")
            ? "USD"
            : salaryCorpus.Contains("eur") || salaryCorpus.Contains("€")
                ? "EUR"
                : salaryCorpus.Contains("cop") || salaryCorpus.Contains("colomb")
                    ? "COP"
                    : null;

        return numbers.Length switch
        {
            0 => (null, null, currency),
            1 => (numbers[0], numbers[0], currency),
            _ => (Math.Min(numbers[0], numbers[1]), Math.Max(numbers[0], numbers[1]), currency)
        };
    }

    private static LanguageProficiency ResolveLanguageProficiency(string corpus)
    {
        if (corpus.Contains("c2"))
        {
            return LanguageProficiency.C2;
        }

        if (corpus.Contains("c1") || corpus.Contains("advanced english") || corpus.Contains("ingles avanzado"))
        {
            return LanguageProficiency.C1;
        }

        if (corpus.Contains("b2") || corpus.Contains("intermediate english") || corpus.Contains("ingles intermedio"))
        {
            return LanguageProficiency.B2;
        }

        if (corpus.Contains("b1"))
        {
            return LanguageProficiency.B1;
        }

        return LanguageProficiency.Unknown;
    }

    private static List<string> BuildQualityFlags(
        JobOffer jobOffer,
        IReadOnlyCollection<JobOfferSkillTag> skillTags,
        IReadOnlyCollection<JobOfferLanguageRequirement> languageRequirements,
        WorkMode workMode,
        string? countryCode)
    {
        var flags = new List<string>();

        if (string.IsNullOrWhiteSpace(jobOffer.Description))
        {
            flags.Add("missing_description");
        }
        else if (jobOffer.Description.Length < 150)
        {
            flags.Add("thin_description");
        }

        if (skillTags.Count == 0)
        {
            flags.Add("missing_skill_tags");
        }
        else if (skillTags.All(x => !x.IsRequired))
        {
            flags.Add("missing_required_skill_signals");
        }

        if (languageRequirements.Count == 0)
        {
            flags.Add("missing_language_signals");
        }

        if (jobOffer.SalaryMin is null && jobOffer.SalaryMax is null)
        {
            flags.Add("missing_salary");
        }
        else if (string.IsNullOrWhiteSpace(jobOffer.SalaryCurrency))
        {
            flags.Add("missing_salary_currency");
        }

        if (workMode == WorkMode.Unknown)
        {
            flags.Add("missing_work_mode");
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            flags.Add("missing_structured_location");
        }

        if (jobOffer.SeniorityLevel == SeniorityLevel.Unknown)
        {
            flags.Add("missing_seniority");
        }

        return flags;
    }

    private static decimal CalculateQualityScore(
        JobOffer jobOffer,
        int skillCount,
        int languageCount,
        int flagCount,
        WorkMode workMode,
        string? countryCode)
    {
        decimal score = 0.15m;

        if (!string.IsNullOrWhiteSpace(jobOffer.Description))
        {
            score += jobOffer.Description.Length >= 150 ? 0.18m : 0.1m;
        }

        if (skillCount > 0)
        {
            score += skillCount >= 3 ? 0.24m : 0.16m;
        }

        if (jobOffer.SalaryMin.HasValue || jobOffer.SalaryMax.HasValue)
        {
            score += !string.IsNullOrWhiteSpace(jobOffer.SalaryCurrency) ? 0.14m : 0.08m;
        }

        if (workMode != WorkMode.Unknown)
        {
            score += 0.08m;
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            score += 0.08m;
        }

        if (jobOffer.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += 0.08m;
        }

        if (languageCount > 0)
        {
            score += 0.05m;
        }

        score -= Math.Min(0.3m, flagCount * 0.03m);

        return Math.Clamp(score, 0.05m, 0.99m);
    }

    private static decimal? ParseDecimal(string value)
    {
        var sanitized = value.Replace(" ", string.Empty);
        if (sanitized.Contains('.') && sanitized.Contains(','))
        {
            sanitized = sanitized.Replace(".", string.Empty).Replace(",", ".");
        }
        else if (sanitized.Count(x => x == '.') > 1)
        {
            sanitized = sanitized.Replace(".", string.Empty);
        }
        else if (sanitized.Count(x => x == ',') > 1)
        {
            sanitized = sanitized.Replace(",", string.Empty);
        }
        else if (sanitized.Contains(','))
        {
            sanitized = sanitized.Replace(",", ".");
        }

        return decimal.TryParse(sanitized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    [GeneratedRegex(@"(?<!\w)\d{1,3}(?:[.,\s]\d{3})*(?:[.,]\d+)?(?!\w)", RegexOptions.Compiled)]
    private static partial Regex SalaryNumberRegex();
}
