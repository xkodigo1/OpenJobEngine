using System.Globalization;
using System.Text.RegularExpressions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Catalog;

namespace OpenJobEngine.Infrastructure.Services;

public sealed partial class DefaultJobEnrichmentService : IJobEnrichmentService
{
    private static readonly string[] SalaryKeywords =
    [
        "salary",
        "salario",
        "sueldo",
        "compensation",
        "compensacion",
        "pay",
        "remuneracion",
        "remuneración",
        "usd",
        "us$",
        "eur",
        "cop",
        "mxn",
        "ars",
        "clp",
        "pen",
        "brl",
        "soles",
        "pesos",
        "dolares",
        "dólares"
    ];

    private static readonly string[] UnsupportedSalaryPeriodKeywords =
    [
        "hourly",
        "per hour",
        "/hr",
        "/hour",
        "por hora",
        "hora",
        "hour rate"
    ];

    private static readonly IReadOnlyDictionary<string, string> CurrencyByCountryCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CO"] = "COP",
        ["MX"] = "MXN",
        ["AR"] = "ARS",
        ["CL"] = "CLP",
        ["PE"] = "PEN",
        ["BR"] = "BRL",
        ["UY"] = "UYU"
    };

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
        var (city, region, countryCode, timeZone) = ResolveLocation(jobOffer.LocationText, rawJobOffer.Metadata);
        var skillTags = ExtractSkillTags(jobOffer.Id, corpus);
        var languageRequirements = ExtractLanguageRequirements(jobOffer.Id, corpus);
        var seniorityLevel = ResolveSeniority(corpus, jobOffer.SeniorityLevel);
        var salary = ResolveSalary(jobOffer, rawJobOffer, countryCode);
        var qualityFlags = BuildQualityFlags(jobOffer, skillTags, languageRequirements, workMode, city, countryCode, salary);
        var qualityScore = CalculateQualityScore(jobOffer, skillTags.Count, languageRequirements.Count, qualityFlags, workMode, city, countryCode, salary);

        jobOffer.SetSeniorityLevel(seniorityLevel);
        jobOffer.SetLocation(city, region, countryCode, timeZone, workMode);
        jobOffer.SetSalary(jobOffer.SalaryText, salary.SalaryMin, salary.SalaryMax, salary.Currency);
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

    private (string? City, string? Region, string? CountryCode, string? TimeZone) ResolveLocation(string? locationText, IReadOnlyDictionary<string, string> metadata)
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
                    return (location.City, location.Region, location.CountryCode, location.TimeZone);
                }
            }
        }

        return (null, null, null, null);
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

        if (corpus.Contains("lead") || corpus.Contains("principal") || corpus.Contains("staff"))
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

    private static SalaryNormalizationResult ResolveSalary(JobOffer jobOffer, RawJobOffer rawJobOffer, string? countryCode)
    {
        var existingCurrency = ResolveCurrency(jobOffer.SalaryText, countryCode);
        if (jobOffer.SalaryMin.HasValue || jobOffer.SalaryMax.HasValue)
        {
            return new SalaryNormalizationResult(
                jobOffer.SalaryMin,
                jobOffer.SalaryMax,
                string.IsNullOrWhiteSpace(jobOffer.SalaryCurrency) ? existingCurrency.Currency : jobOffer.SalaryCurrency,
                string.IsNullOrWhiteSpace(jobOffer.SalaryCurrency) && existingCurrency.IsInferred,
                false,
                false,
                false);
        }

        var salarySource = BuildSalarySource(jobOffer, rawJobOffer);
        if (string.IsNullOrWhiteSpace(salarySource))
        {
            return SalaryNormalizationResult.Empty;
        }

        var currency = ResolveCurrency(salarySource, countryCode);
        var normalizedSalarySource = salarySource.ToLowerInvariant();
        var hasUnsupportedPeriod = UnsupportedSalaryPeriodKeywords.Any(keyword => normalizedSalarySource.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (hasUnsupportedPeriod)
        {
            return new SalaryNormalizationResult(
                null,
                null,
                currency.Currency,
                currency.IsInferred,
                false,
                true,
                true);
        }

        var amounts = SalaryTokenRegex().Matches(salarySource)
            .Select(match => NormalizeSalaryAmount(match.Groups["amount"].Value, match.Groups["suffix"].Value))
            .Where(amount => amount.HasValue)
            .Select(amount => amount!.Value)
            .Where(amount => amount > 0)
            .Take(4)
            .ToArray();

        if (amounts.Length == 0)
        {
            return new SalaryNormalizationResult(null, null, currency.Currency, currency.IsInferred, false, true, false);
        }

        if (amounts.Any(amount => amount < 10))
        {
            return new SalaryNormalizationResult(null, null, currency.Currency, currency.IsInferred, false, true, false);
        }

        var salaryMin = amounts.Min();
        var salaryMax = amounts.Max();
        var hasSingleAmount = amounts.Length == 1;
        var isOutlier = IsSalaryOutlier(salaryMin, salaryMax, currency.Currency);

        return new SalaryNormalizationResult(
            salaryMin,
            hasSingleAmount ? salaryMin : salaryMax,
            currency.Currency,
            currency.IsInferred,
            isOutlier,
            false,
            false);
    }

    private static string? BuildSalarySource(JobOffer jobOffer, RawJobOffer rawJobOffer)
    {
        var fragments = new List<string>();

        if (!string.IsNullOrWhiteSpace(jobOffer.SalaryText))
        {
            fragments.Add(jobOffer.SalaryText);
        }

        if (rawJobOffer.Metadata.TryGetValue("salary", out var salaryMetadata) &&
            !string.IsNullOrWhiteSpace(salaryMetadata))
        {
            fragments.Add(salaryMetadata);
        }

        if (!string.IsNullOrWhiteSpace(jobOffer.Description))
        {
            var salarySnippets = jobOffer.Description
                .Split(['\r', '\n', '.', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(ContainsSalaryHint)
                .ToArray();

            if (salarySnippets.Length > 0)
            {
                fragments.AddRange(salarySnippets);
            }
        }

        return fragments.Count == 0
            ? null
            : string.Join(" ", fragments.Where(fragment => !string.IsNullOrWhiteSpace(fragment)));
    }

    private static bool ContainsSalaryHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.ToLowerInvariant();
        return SalaryKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
               normalized.Contains("$", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("€", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("s/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("r$", StringComparison.OrdinalIgnoreCase);
    }

    private static (string? Currency, bool IsInferred) ResolveCurrency(string? salaryText, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(salaryText))
        {
            return TryInferCurrencyFromCountry(countryCode);
        }

        var normalized = salaryText.ToLowerInvariant();
        if (normalized.Contains("usd") || normalized.Contains("us$") || normalized.Contains("dolares") || normalized.Contains("dólares"))
        {
            return ("USD", false);
        }

        if (normalized.Contains("eur") || normalized.Contains("€"))
        {
            return ("EUR", false);
        }

        if (normalized.Contains("cop") || normalized.Contains("colombian peso") || normalized.Contains("colombianos"))
        {
            return ("COP", false);
        }

        if (normalized.Contains("mxn") || normalized.Contains("mex$") || normalized.Contains("pesos mexicanos"))
        {
            return ("MXN", false);
        }

        if (normalized.Contains("ars") || normalized.Contains("ar$") || normalized.Contains("pesos argentinos"))
        {
            return ("ARS", false);
        }

        if (normalized.Contains("clp") || normalized.Contains("pesos chilenos"))
        {
            return ("CLP", false);
        }

        if (normalized.Contains("pen") || normalized.Contains("s/") || normalized.Contains("soles"))
        {
            return ("PEN", false);
        }

        if (normalized.Contains("brl") || normalized.Contains("r$") || normalized.Contains("reales"))
        {
            return ("BRL", false);
        }

        if (normalized.Contains("$", StringComparison.OrdinalIgnoreCase) || normalized.Contains("peso", StringComparison.OrdinalIgnoreCase))
        {
            return TryInferCurrencyFromCountry(countryCode);
        }

        return (null, false);
    }

    private static (string? Currency, bool IsInferred) TryInferCurrencyFromCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return (null, false);
        }

        return CurrencyByCountryCode.TryGetValue(countryCode.Trim(), out var currency)
            ? (currency, true)
            : (null, false);
    }

    private static decimal? NormalizeSalaryAmount(string amountText, string suffixText)
    {
        var amount = ParseDecimal(amountText);
        if (!amount.HasValue)
        {
            return null;
        }

        var normalizedSuffix = suffixText.Trim().ToLowerInvariant();
        var multiplier = normalizedSuffix switch
        {
            "k" => 1_000m,
            "mil" => 1_000m,
            "thousand" => 1_000m,
            "m" => 1_000_000m,
            "mm" => 1_000_000m,
            "million" => 1_000_000m,
            "millon" => 1_000_000m,
            "millones" => 1_000_000m,
            _ => 1m
        };

        return decimal.Round(amount.Value * multiplier, 2);
    }

    private static bool IsSalaryOutlier(decimal salaryMin, decimal salaryMax, string? currency)
    {
        if (salaryMax <= 0)
        {
            return false;
        }

        return currency?.ToUpperInvariant() switch
        {
            "USD" or "EUR" => salaryMax > 1_000_000m,
            "COP" => salaryMax > 1_000_000_000m,
            "MXN" or "ARS" or "CLP" or "PEN" or "BRL" or "UYU" => salaryMax > 500_000_000m,
            _ => false
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
        string? city,
        string? countryCode,
        SalaryNormalizationResult salary)
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

        if (salary.SalaryMin is null && salary.SalaryMax is null)
        {
            flags.Add("missing_salary");
        }
        else if (string.IsNullOrWhiteSpace(salary.Currency))
        {
            flags.Add("missing_salary_currency");
        }

        if (salary.IsCurrencyInferred)
        {
            flags.Add("salary_currency_inferred");
        }

        if (salary.IsAmbiguous)
        {
            flags.Add("salary_amount_ambiguous");
        }

        if (salary.HasUnsupportedPeriod)
        {
            flags.Add("salary_period_unsupported");
        }

        if (salary.IsOutlier)
        {
            flags.Add("salary_amount_outlier");
        }

        if (workMode == WorkMode.Unknown)
        {
            flags.Add("missing_work_mode");
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            flags.Add("missing_structured_location");
        }
        else if (string.Equals(countryCode, "LATAM", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(city, "Remote", StringComparison.OrdinalIgnoreCase))
        {
            flags.Add("broad_location_only");
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
        IReadOnlyCollection<string> qualityFlags,
        WorkMode workMode,
        string? city,
        string? countryCode,
        SalaryNormalizationResult salary)
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

        if (salary.SalaryMin.HasValue || salary.SalaryMax.HasValue)
        {
            score += !string.IsNullOrWhiteSpace(salary.Currency) && !salary.IsCurrencyInferred ? 0.14m : 0.08m;
        }

        if (workMode != WorkMode.Unknown)
        {
            score += 0.08m;
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            score += string.Equals(countryCode, "LATAM", StringComparison.OrdinalIgnoreCase) ? 0.04m : 0.08m;
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            score += 0.03m;
        }

        if (jobOffer.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += 0.08m;
        }

        if (languageCount > 0)
        {
            score += 0.05m;
        }

        var penalty = 0m;
        foreach (var flag in qualityFlags)
        {
            penalty += flag switch
            {
                "salary_currency_inferred" => 0.01m,
                "broad_location_only" => 0.02m,
                "thin_description" => 0.03m,
                "salary_amount_ambiguous" or "salary_period_unsupported" or "salary_amount_outlier" => 0.05m,
                _ => 0.03m
            };
        }

        score -= Math.Min(0.32m, penalty);
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

    private sealed record SalaryNormalizationResult(
        decimal? SalaryMin,
        decimal? SalaryMax,
        string? Currency,
        bool IsCurrencyInferred,
        bool IsOutlier,
        bool IsAmbiguous,
        bool HasUnsupportedPeriod)
    {
        public static SalaryNormalizationResult Empty { get; } = new(null, null, null, false, false, false, false);
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9])(?<amount>\d{1,3}(?:[.,\s]\d{3})*(?:[.,]\d+)?)(?:\s*(?<suffix>k|m|mm|mil|thousand|million|millon|millones))?(?![A-Za-z0-9])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SalaryTokenRegex();
}
