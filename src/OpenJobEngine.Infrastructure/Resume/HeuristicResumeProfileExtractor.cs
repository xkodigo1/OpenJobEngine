using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;
using OpenJobEngine.Infrastructure.Catalog;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Resume;

public sealed partial class HeuristicResumeProfileExtractor : IResumeProfileExtractor
{
    private static readonly Regex ExplicitYearsRegex = ExplicitYearsPattern();
    private static readonly Regex SinceYearRegex = SinceYearPattern();
    private static readonly Regex RangeYearsRegex = RangeYearsPattern();
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
        var lines = SplitLines(resumeText);
        var sections = DetectSections(lines);
        var normalizedSections = sections.Sections.ToDictionary(
            x => x.Key,
            x => NormalizeForComparison(x.Value),
            StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var confidences = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var detectedSkills = ExtractSkills(normalizedSections);
        confidences["skills"] = CalculateSkillsConfidence(detectedSkills, sections);

        var detectedLanguages = ExtractLanguages(sections);
        confidences["languages"] = CalculateLanguagesConfidence(detectedLanguages, sections);

        var years = ExtractYears(sections, normalizedSections);
        confidences["yearsOfExperience"] = CalculateYearsConfidence(years, sections);

        var title = InferTitle(sections, normalizedSections, detectedSkills);
        confidences["targetTitle"] = CalculateTitleConfidence(title, detectedSkills, sections, years);

        var seniority = InferSeniority(normalizedSections, years);
        confidences["seniorityLevel"] = seniority == SeniorityLevel.Unknown ? 0.15m : 0.72m;

        var location = InferLocation(normalizedSections);
        confidences["location"] = location.city is null ? 0.0m : 0.78m;

        var summary = BuildSummary(sections);
        confidences["professionalSummary"] = summary is null ? 0.0m : 0.82m;

        if (!sections.HasExplicit("experience"))
        {
            warnings.Add("No se identifico una seccion de experiencia clara.");
        }

        if (!sections.HasExplicit("languages") && detectedLanguages.Count == 0)
        {
            warnings.Add("No se identifico una seccion de idiomas clara.");
        }

        if (detectedSkills.Count == 0)
        {
            warnings.Add("No se detectaron skills tecnicas claras en el CV.");
        }

        if (detectedLanguages.Count == 0)
        {
            warnings.Add("No se detectaron idiomas de forma confiable.");
        }

        if (!years.HasValue)
        {
            warnings.Add("No se pudo inferir la experiencia total.");
        }

        if (location.city is null)
        {
            warnings.Add("No se pudo inferir una ciudad actual de forma confiable.");
        }

        if (seniority == SeniorityLevel.Unknown)
        {
            warnings.Add("El seniority no se pudo inferir con suficiente confianza.");
        }

        if (summary is null)
        {
            warnings.Add("No se identifico un resumen profesional explicito.");
        }

        var suggestedProfile = new CandidateProfileUpsertRequest(
            title,
            summary,
            years ?? 0,
            seniority.ToString(),
            WorkMode.Unknown.ToString(),
            false,
            false,
            false,
            null,
            null,
            null,
            location.city,
            location.region,
            location.countryCode,
            Array.Empty<string>(),
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
            sections.Sections,
            detectedSkills.Select(x => x.SkillName).ToArray(),
            detectedLanguages.Select(x => x.LanguageName).ToArray(),
            years);
    }

    private IReadOnlyCollection<CandidateSkillInput> ExtractSkills(IReadOnlyDictionary<string, string> normalizedSections)
    {
        var source = CombineSections(normalizedSections, "skills", "experience", "summary");
        if (string.IsNullOrWhiteSpace(source))
        {
            source = CombineSections(normalizedSections);
        }

        return skills
            .Where(skill => skill.Tokens.Concat(skill.Aliases).Any(token => ContainsToken(source, token)))
            .Select(skill => new CandidateSkillInput(skill.Name, skill.Slug, skill.Category.ToString(), null, 4))
            .DistinctBy(x => x.SkillSlug, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyCollection<CandidateLanguageInput> ExtractLanguages(SectionExtraction sections)
    {
        var source = sections.HasExplicit("languages")
            ? sections.Sections.GetValueOrDefault("languages", string.Empty)
            : CombineSections(sections.Sections, "summary", "experience", "skills");

        if (string.IsNullOrWhiteSpace(source))
        {
            source = CombineSections(sections.Sections);
        }

        var entries = SplitLanguageEntries(source);

        return languages
            .Select(language =>
            {
                var entry = entries.FirstOrDefault(candidate =>
                {
                    var normalizedCandidate = NormalizeForComparison(candidate);
                    return language.Tokens.Any(token => ContainsToken(normalizedCandidate, token)) ||
                           ContainsToken(normalizedCandidate, language.Name) ||
                           ContainsToken(normalizedCandidate, language.Code);
                });

                return string.IsNullOrWhiteSpace(entry)
                    ? null
                    : new CandidateLanguageInput(
                        language.Code,
                        language.Name,
                        InferLanguageProficiency(entry));
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .DistinctBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static decimal CalculateSkillsConfidence(IReadOnlyCollection<CandidateSkillInput> detectedSkills, SectionExtraction sections)
    {
        if (detectedSkills.Count == 0)
        {
            return 0.0m;
        }

        var score = 0.35m + (detectedSkills.Count * 0.08m);
        if (sections.HasExplicit("skills"))
        {
            score += 0.15m;
        }

        return Math.Min(0.95m, score);
    }

    private static decimal CalculateLanguagesConfidence(IReadOnlyCollection<CandidateLanguageInput> detectedLanguages, SectionExtraction sections)
    {
        if (detectedLanguages.Count == 0)
        {
            return 0.0m;
        }

        var score = sections.HasExplicit("languages") ? 0.78m : 0.58m;
        if (detectedLanguages.Any(x => !string.Equals(x.Proficiency, "Unknown", StringComparison.OrdinalIgnoreCase)))
        {
            score += 0.08m;
        }

        return Math.Min(0.95m, score);
    }

    private static decimal CalculateYearsConfidence(decimal? years, SectionExtraction sections)
    {
        if (!years.HasValue)
        {
            return 0.0m;
        }

        if (sections.HasExplicit("experience"))
        {
            return 0.88m;
        }

        return 0.65m;
    }

    private static decimal CalculateTitleConfidence(string title, IReadOnlyCollection<CandidateSkillInput> detectedSkills, SectionExtraction sections, decimal? years)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return 0.0m;
        }

        if (IsGenericTitle(title))
        {
            return 0.35m;
        }

        if (sections.HasExplicit("summary") || sections.HasExplicit("experience"))
        {
            return 0.84m;
        }

        if (detectedSkills.Count > 0 || years.HasValue)
        {
            return 0.72m;
        }

        return 0.5m;
    }

    private static bool IsGenericTitle(string title)
    {
        return title.Equals("Software Engineer", StringComparison.OrdinalIgnoreCase) ||
               title.Equals("Software Developer", StringComparison.OrdinalIgnoreCase) ||
               title.Equals("Technology Professional", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForComparison(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return Regex.Replace(
            builder.ToString()
                .Replace('\u2013', '-')
                .Replace('\u2014', '-')
                .Replace('\u2022', ' '),
            @"\s+",
            " ").Trim();
    }

    private static IReadOnlyList<string> SplitLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static SectionExtraction DetectSections(IReadOnlyList<string> lines)
    {
        var sectionHeads = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["summary"] = ["summary", "perfil", "sobre mi", "about", "professional summary"],
            ["experience"] = ["experience", "experiencia", "work experience", "employment"],
            ["skills"] = ["skills", "habilidades", "stack", "technologies", "tecnologias"],
            ["languages"] = ["languages", "idiomas"],
            ["education"] = ["education", "educacion", "formacion", "studies"]
        };

        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var explicitSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string currentSection = "summary";
        sections[currentSection] = [];

        foreach (var rawLine in lines)
        {
            var normalizedLine = NormalizeForComparison(rawLine);
            var detectedSection = sectionHeads.FirstOrDefault(section => section.Value.Any(alias => IsSectionHeading(normalizedLine, alias))).Key;

            if (!string.IsNullOrWhiteSpace(detectedSection))
            {
                currentSection = detectedSection;
                explicitSections.Add(currentSection);
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = [];
                }

                continue;
            }

            sections[currentSection].Add(rawLine.Trim());
        }

        return new SectionExtraction(
            sections.ToDictionary(
                x => x.Key,
                x => string.Join(Environment.NewLine, x.Value).Trim(),
                StringComparer.OrdinalIgnoreCase),
            explicitSections);
    }

    private static bool IsSectionHeading(string normalizedLine, string alias)
    {
        if (string.IsNullOrWhiteSpace(normalizedLine) || string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        var candidate = normalizedLine.Trim().TrimEnd(':', '-').Trim();
        if (candidate.Length > 42)
        {
            return false;
        }

        return candidate.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(alias + ":", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(alias + " -", StringComparison.OrdinalIgnoreCase) ||
               (candidate.StartsWith(alias + " ", StringComparison.OrdinalIgnoreCase) && candidate.Length <= alias.Length + 12);
    }

    private static decimal? ExtractYears(SectionExtraction sections, IReadOnlyDictionary<string, string> normalizedSections)
    {
        var searchSpaces = new[]
        {
            normalizedSections.GetValueOrDefault("experience", string.Empty),
            normalizedSections.GetValueOrDefault("summary", string.Empty)
        };

        var explicitYears = new List<decimal>();
        var derivedYears = new List<decimal>();
        foreach (var space in searchSpaces.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            foreach (Match match in ExplicitYearsRegex.Matches(space))
            {
                if (decimal.TryParse(match.Groups["years"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    explicitYears.Add(parsed);
                }

                if (match.Groups["years2"].Success &&
                    decimal.TryParse(match.Groups["years2"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedRange) &&
                    parsedRange > 0)
                {
                    explicitYears.Add(parsedRange);
                }
            }

            foreach (Match match in RangeYearsRegex.Matches(space))
            {
                if (decimal.TryParse(match.Groups["years"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    explicitYears.Add(parsed);
                }
            }

            foreach (Match match in SinceYearRegex.Matches(space))
            {
                if (int.TryParse(match.Groups["year"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var year) &&
                    year >= 1980 &&
                    year <= DateTime.UtcNow.Year)
                {
                    derivedYears.Add(DateTime.UtcNow.Year - year);
                }
            }
        }

        if (explicitYears.Count > 0)
        {
            return explicitYears.Min();
        }

        return derivedYears.Count == 0 ? null : derivedYears.Min();
    }

    private static string InferTitle(SectionExtraction sections, IReadOnlyDictionary<string, string> normalizedSections, IReadOnlyCollection<CandidateSkillInput> detectedSkills)
    {
        var titleSource = CombineSections(normalizedSections, "summary", "experience");
        if (string.IsNullOrWhiteSpace(titleSource))
        {
            titleSource = CombineSections(normalizedSections);
        }

        var rolePatterns = new (string Pattern, string Title)[]
        {
            ("full stack", "Full Stack Developer"),
            ("fullstack", "Full Stack Developer"),
            ("backend", "Backend Developer"),
            ("back end", "Backend Developer"),
            ("frontend", "Frontend Developer"),
            ("front end", "Frontend Developer"),
            ("devops", "DevOps Engineer"),
            ("site reliability", "SRE Engineer"),
            ("platform engineer", "Platform Engineer"),
            ("data engineer", "Data Engineer"),
            ("data scientist", "Data Scientist"),
            ("machine learning", "ML Engineer"),
            ("mobile", "Mobile Engineer"),
            ("android", "Mobile Engineer"),
            ("ios", "Mobile Engineer"),
            ("qa", "QA Engineer"),
            ("test automation", "QA Automation Engineer"),
            ("software architect", "Software Architect"),
            ("solution architect", "Software Architect"),
            ("product manager", "Product Manager"),
            ("engineering manager", "Engineering Manager"),
            ("cto", "Executive")
        };

        foreach (var (pattern, title) in rolePatterns)
        {
            if (titleSource.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return title;
            }
        }

        if (detectedSkills.Any(x => x.SkillSlug is "csharp" or "nodejs" or "python" or "java" or "javascript" or "typescript" or "php" or "go"))
        {
            return "Software Engineer";
        }

        if (sections.HasExplicit("experience") || sections.HasExplicit("summary"))
        {
            return "Technology Professional";
        }

        return "Technology Professional";
    }

    private static SeniorityLevel InferSeniority(IReadOnlyDictionary<string, string> normalizedSections, decimal? years)
    {
        var titleSource = CombineSections(normalizedSections, "summary", "experience");
        if (string.IsNullOrWhiteSpace(titleSource))
        {
            titleSource = CombineSections(normalizedSections);
        }

        if (ContainsAny(titleSource, ["executive", "vp", "vice president", "chief", "cto", "ceo", "head of"]))
        {
            return SeniorityLevel.Executive;
        }

        if (ContainsAny(titleSource, ["lead", "principal", "staff", "architect"]))
        {
            return SeniorityLevel.Lead;
        }

        if (ContainsAny(titleSource, ["senior", "sr", "sr.", "senior engineer"]))
        {
            return SeniorityLevel.Senior;
        }

        if (ContainsAny(titleSource, ["semi senior", "semi-senior", "mid", "mid-level", "intermediate", "ssr", "ssr."]))
        {
            return SeniorityLevel.Mid;
        }

        if (ContainsAny(titleSource, ["junior", "jr", "jr.", "entry", "trainee", "intern"]))
        {
            return SeniorityLevel.Junior;
        }

        if (!years.HasValue)
        {
            return SeniorityLevel.Unknown;
        }

        if (years.Value >= 8)
        {
            return SeniorityLevel.Senior;
        }

        if (years.Value >= 4)
        {
            return SeniorityLevel.Mid;
        }

        if (years.Value > 0)
        {
            return SeniorityLevel.Junior;
        }

        return SeniorityLevel.Unknown;
    }

    private (string? city, string? region, string? countryCode) InferLocation(IReadOnlyDictionary<string, string> normalizedSections)
    {
        var sourceCandidates = new[]
        {
            normalizedSections.GetValueOrDefault("summary", string.Empty),
            normalizedSections.GetValueOrDefault("experience", string.Empty),
            CombineSections(normalizedSections)
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToArray();

        foreach (var location in locations.OrderByDescending(x => x.Aliases.DefaultIfEmpty(string.Empty).Max(alias => alias.Length)))
        {
            var aliasMatches = sourceCandidates.Any(source => location.Aliases.Any(alias => ContainsToken(source, alias)));
            if (!aliasMatches)
            {
                continue;
            }

            if (location.CountryCode.Equals("LATAM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return (location.City, location.Region, location.CountryCode);
        }

        return (null, null, null);
    }

    private static string? BuildSummary(SectionExtraction sections)
    {
        var summary = sections.Sections.GetValueOrDefault("summary", string.Empty);
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var normalized = NormalizeForComparison(summary);
        if (!sections.HasExplicit("summary") && normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 8)
        {
            return null;
        }

        if (ContainsAny(normalized, ["@", "linkedin", "github", "phone", "telefono", "celular", "whatsapp"]))
        {
            return null;
        }

        var trimmed = summary.Trim();
        return trimmed.Length > 400 ? trimmed[..400] : trimmed;
    }

    private static string InferLanguageProficiency(string entry)
    {
        var normalized = NormalizeForComparison(entry);

        if (ContainsAny(normalized, ["native", "nativo", "mother tongue", "first language", "bilingual"]))
        {
            return "Native";
        }

        if (ContainsAny(normalized, ["c2", "near native", "fluent", "fluent english", "fluido"]))
        {
            return "C2";
        }

        if (ContainsAny(normalized, ["c1", "advanced", "avanzado", "proficient", "profesional"]))
        {
            return "C1";
        }

        if (ContainsAny(normalized, ["b2", "upper intermediate", "upper-intermediate", "intermedio alto"]))
        {
            return "B2";
        }

        if (ContainsAny(normalized, ["b1", "intermediate", "intermedio", "conversational", "working"]))
        {
            return "B1";
        }

        if (ContainsAny(normalized, ["a2", "basic", "elementary"]))
        {
            return "A2";
        }

        if (ContainsAny(normalized, ["a1"]))
        {
            return "A1";
        }

        return "Unknown";
    }

    private static bool ContainsAny(string source, IReadOnlyCollection<string> terms)
    {
        return terms.Any(term => ContainsToken(source, term));
    }

    private static bool ContainsToken(string source, string token)
    {
        var normalizedToken = NormalizeForComparison(token);
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(normalizedToken))
        {
            return false;
        }

        var pattern = $"(?<![a-z0-9]){Regex.Escape(normalizedToken)}(?![a-z0-9])";
        return Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string CombineSections(IReadOnlyDictionary<string, string> sections, params string[] names)
    {
        if (names.Length == 0)
        {
            return string.Join(" ", sections.Values.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return string.Join(" ", names.Select(name => sections.GetValueOrDefault(name, string.Empty)).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static IReadOnlyList<string> SplitLanguageEntries(string source)
    {
        return source
            .Split(['\r', '\n', ';', '|', '/', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .ToArray();
    }

    private sealed record SectionExtraction(
        IReadOnlyDictionary<string, string> Sections,
        IReadOnlyCollection<string> ExplicitSections)
    {
        public bool HasExplicit(string name) => ExplicitSections.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?<years>\d{1,2})(?:\+)?\s*(?:years?|years? of experience|year|anos?|de experiencia)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitYearsPattern();

    [GeneratedRegex(@"(?:since|desde)\s+(?<year>(?:19|20)\d{2})", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SinceYearPattern();

    [GeneratedRegex(@"(?<years>\d{1,2})\s*(?:to|-)\s*(?<years2>\d{1,2})\s*(?:years?|anos?)(?:\s+of\s+experience)?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RangeYearsPattern();
}
