using System.Text.RegularExpressions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;
using OpenJobEngine.Infrastructure.Catalog;

namespace OpenJobEngine.Infrastructure.Resume;

public sealed partial class HeuristicResumeProfileExtractor : IResumeProfileExtractor
{
    private static readonly Regex YearsRegex = YearsPattern();
    private readonly IReadOnlyCollection<CatalogSkillDefinition> skills;
    private readonly IReadOnlyCollection<CatalogLanguageDefinition> languages;
    private readonly IReadOnlyCollection<CatalogLocationDefinition> locations;

    public HeuristicResumeProfileExtractor(ITechnologyTaxonomyProvider taxonomyProvider)
    {
        skills = taxonomyProvider.GetSkills();
        languages = taxonomyProvider.GetLanguages();
        locations = taxonomyProvider.GetLocations();
    }

    public ResumeProfileExtractionResultDto Extract(string resumeText)
    {
        var normalized = NormalizeText(resumeText);
        var sections = DetectSections(resumeText);
        var warnings = new List<string>();
        var confidences = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var detectedSkills = skills
            .Where(skill => skill.Tokens.Concat(skill.Aliases).Any(token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Select(skill => new CandidateSkillInput(skill.Name, skill.Slug, skill.Category.ToString(), null, 4))
            .DistinctBy(x => x.SkillSlug, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        confidences["skills"] = detectedSkills.Length == 0 ? 0.2m : Math.Min(0.95m, 0.45m + (detectedSkills.Length * 0.08m));

        var detectedLanguages = languages
            .Where(language => language.Tokens.Any(token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Select(language => new CandidateLanguageInput(language.Code, language.Name, InferProficiency(normalized, language.Code)))
            .DistinctBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        confidences["languages"] = detectedLanguages.Length == 0 ? 0.2m : 0.75m;

        var years = ExtractYears(normalized, sections);
        confidences["yearsOfExperience"] = years.HasValue ? 0.7m : 0.25m;

        var title = InferTitle(normalized, detectedSkills, sections);
        confidences["targetTitle"] = string.IsNullOrWhiteSpace(title) ? 0.2m : 0.7m;

        var seniority = InferSeniority(normalized, years);
        confidences["seniorityLevel"] = seniority == "Unknown" ? 0.25m : 0.68m;

        var city = InferCity(normalized);
        var countryCode = locations.FirstOrDefault(x => string.Equals(x.City, city, StringComparison.OrdinalIgnoreCase))?.CountryCode;
        confidences["location"] = city is null ? 0.2m : 0.65m;

        if (detectedSkills.Length == 0)
        {
            warnings.Add("No se detectaron skills tecnicas claras en el CV.");
        }

        if (detectedLanguages.Length == 0)
        {
            warnings.Add("No se detectaron idiomas de forma confiable.");
        }

        if (!years.HasValue)
        {
            warnings.Add("La experiencia total es ambigua y fue estimada como 0.");
        }

        if (city is null)
        {
            warnings.Add("No se pudo inferir una ciudad actual de forma confiable.");
        }

        if (seniority == "Unknown")
        {
            warnings.Add("El seniority no se pudo inferir con suficiente confianza.");
        }

        var suggestedProfile = new CandidateProfileUpsertRequest(
            title,
            BuildSummary(sections, resumeText),
            years ?? 0,
            seniority,
            "Remote",
            true,
            true,
            true,
            null,
            null,
            null,
            city,
            null,
            countryCode,
            city is null ? Array.Empty<string>() : [city],
            Array.Empty<string>(),
            Array.Empty<string>(),
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            detectedSkills,
            detectedLanguages);

        return new ResumeProfileExtractionResultDto(
            suggestedProfile,
            confidences,
            warnings,
            sections,
            detectedSkills.Select(x => x.SkillName).ToArray(),
            detectedLanguages.Select(x => x.LanguageName).ToArray(),
            years);
    }

    private static string NormalizeText(string text)
    {
        return text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("años", "anos", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, string> DetectSections(string text)
    {
        var sectionHeads = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["summary"] = ["summary", "perfil", "sobre mi", "about", "professional summary"],
            ["experience"] = ["experience", "experiencia", "work experience", "employment"],
            ["skills"] = ["skills", "habilidades", "stack", "technologies", "tecnologias"],
            ["languages"] = ["languages", "idiomas"],
            ["education"] = ["education", "educacion", "formacion", "studies"]
        };

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string currentSection = "summary";
        sections[currentSection] = [];

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            var normalizedLine = line.ToLowerInvariant();
            var detectedSection = sectionHeads
                .FirstOrDefault(section => section.Value.Any(alias => normalizedLine.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                .Key;

            if (!string.IsNullOrWhiteSpace(detectedSection))
            {
                currentSection = detectedSection;
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = [];
                }

                continue;
            }

            sections[currentSection].Add(line);
        }

        return sections.ToDictionary(
            x => x.Key,
            x => string.Join(" ", x.Value).Trim(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static decimal? ExtractYears(string normalized, IReadOnlyDictionary<string, string> sections)
    {
        var searchSpaces = new[]
        {
            normalized,
            sections.TryGetValue("experience", out var experience) ? experience.ToLowerInvariant() : null,
            sections.TryGetValue("summary", out var summary) ? summary.ToLowerInvariant() : null
        };

        var values = searchSpaces
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(space => YearsRegex.Matches(space!).Select(match => decimal.TryParse(match.Groups["years"].Value, out var parsed) ? parsed : 0))
            .Where(x => x > 0)
            .ToArray();

        return values.Length == 0 ? null : values.Max();
    }

    private static string InferTitle(string normalized, IReadOnlyCollection<CandidateSkillInput> detectedSkills, IReadOnlyDictionary<string, string> sections)
    {
        var titleSource = sections.TryGetValue("summary", out var summary) ? $"{summary} {normalized}" : normalized;

        if (titleSource.Contains("backend"))
        {
            return "Backend Developer";
        }

        if (titleSource.Contains("frontend"))
        {
            return "Frontend Developer";
        }

        if (titleSource.Contains("full stack") || titleSource.Contains("fullstack"))
        {
            return "Full Stack Developer";
        }

        if (titleSource.Contains("devops"))
        {
            return "DevOps Engineer";
        }

        if (detectedSkills.Any(x => x.SkillSlug is "nodejs" or "csharp" or "python" or "java"))
        {
            return "Software Engineer";
        }

        return "Software Developer";
    }

    private static string InferSeniority(string normalized, decimal? years)
    {
        if (normalized.Contains("lead") || normalized.Contains("principal"))
        {
            return "Lead";
        }

        if (normalized.Contains("senior") || (years.HasValue && years.Value >= 5))
        {
            return "Senior";
        }

        if (normalized.Contains("semi senior") || normalized.Contains("semi-senior") || (years.HasValue && years.Value >= 3))
        {
            return "Mid";
        }

        if (normalized.Contains("junior") || (years.HasValue && years.Value > 0))
        {
            return "Junior";
        }

        return "Unknown";
    }

    private static string InferProficiency(string normalized, string languageCode)
    {
        if (normalized.Contains("c2") || normalized.Contains("proficient english"))
        {
            return "C2";
        }

        if (normalized.Contains("c1") || normalized.Contains("advanced english") || normalized.Contains("ingles avanzado"))
        {
            return "C1";
        }

        if (normalized.Contains("b2") || normalized.Contains("intermediate english") || normalized.Contains("ingles intermedio"))
        {
            return "B2";
        }

        if (normalized.Contains("b1"))
        {
            return "B1";
        }

        return languageCode.Equals("es", StringComparison.OrdinalIgnoreCase) ? "Native" : "Unknown";
    }

    private string? InferCity(string normalized)
    {
        foreach (var location in locations)
        {
            if (location.Aliases.Any(alias => normalized.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            {
                return location.City;
            }
        }

        return null;
    }

    private static string? BuildSummary(IReadOnlyDictionary<string, string> sections, string text)
    {
        var source = sections.TryGetValue("summary", out var summary) && !string.IsNullOrWhiteSpace(summary)
            ? summary
            : text.Replace(Environment.NewLine, " ").Trim();

        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        return source.Length > 400 ? source[..400] : source;
    }

    [GeneratedRegex(@"(?<years>\d{1,2})(?:\+)?\s+(?:years|year|anos|a[ñn]os)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex YearsPattern();
}
