using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Application.Matching;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;
using OpenJobEngine.Infrastructure.Catalog;

namespace OpenJobEngine.Infrastructure.Matching;

public sealed class DeterministicMatchingService(
    ICandidateProfileRepository candidateProfileRepository,
    IJobRepository jobRepository,
    IMatchExecutionRepository matchExecutionRepository,
    IMatchingRulesProvider matchingRulesProvider,
    ITechnologyTaxonomyProvider taxonomyProvider,
    IUnitOfWork unitOfWork) : IMatchingService
{
    private readonly IReadOnlyDictionary<string, CatalogSkillDefinition> skillIndex = taxonomyProvider
        .GetSkills()
        .ToDictionary(x => x.Slug, x => x, StringComparer.OrdinalIgnoreCase);

    public MatchingRuleSetDto GetCurrentRules() => matchingRulesProvider.GetCurrent();

    public async Task<MatchingSearchResultDto> SearchAsync(MatchingSearchRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{request.ProfileId}' was not found.");

        var jobs = await jobRepository.ListActiveAsync(cancellationToken);
        var filteredJobs = ApplyFilters(jobs, request);
        var rules = matchingRulesProvider.GetCurrent();
        var matches = filteredJobs
            .Select(job => Score(profile, job, rules, skillIndex))
            .Where(result => !request.MinimumMatchScore.HasValue || result.MatchScore >= request.MinimumMatchScore.Value)
            .OrderByDescending(result => result.MatchScore)
            .ThenByDescending(result => result.Job.PublishedAtUtc ?? result.Job.CollectedAtUtc)
            .ToArray();

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => request.PageSize
        };

        var pagedItems = matches.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        decimal? averageScore = matches.Length == 0 ? null : matches.Average(x => x.MatchScore);
        var highMatchCount = matches.Count(x => string.Equals(x.MatchBand, MatchBand.High.ToString(), StringComparison.OrdinalIgnoreCase));
        var mediumMatchCount = matches.Count(x => string.Equals(x.MatchBand, MatchBand.Medium.ToString(), StringComparison.OrdinalIgnoreCase));
        var lowMatchCount = matches.Count(x => string.Equals(x.MatchBand, MatchBand.Low.ToString(), StringComparison.OrdinalIgnoreCase));

        await matchExecutionRepository.AddAsync(
            new MatchExecution(
                Guid.NewGuid(),
                request.ProfileId,
                request.Query,
                pagedItems.Length,
                pagedItems.FirstOrDefault()?.MatchScore,
                averageScore,
                highMatchCount,
                mediumMatchCount,
                lowMatchCount,
                rules.Version),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MatchingSearchResultDto(
            request.ProfileId,
            rules.Version,
            new PagedResult<JobMatchResultDto>(pagedItems, page, pageSize, matches.Length));
    }

    public async Task<JobMatchResultDto?> GetJobMatchAsync(Guid profileId, Guid jobId, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);

        if (profile is null || job is null || !job.IsActive)
        {
            return null;
        }

        return Score(profile, job, matchingRulesProvider.GetCurrent(), skillIndex);
    }

    private static IReadOnlyCollection<JobOffer> ApplyFilters(IReadOnlyCollection<JobOffer> jobs, MatchingSearchRequest request)
    {
        var query = jobs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(job =>
                job.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                job.CompanyName.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                (job.Description?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(job =>
                (job.LocationText?.Contains(request.Location, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (job.City?.Contains(request.Location, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (request.RemoteOnly == true)
        {
            query = query.Where(job => job.WorkMode == WorkMode.Remote || job.IsRemote);
        }

        if (request.SalaryMin.HasValue)
        {
            query = query.Where(job => job.SalaryMax == null || job.SalaryMax >= request.SalaryMin.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            query = query.Where(job => string.Equals(job.SourceName, request.Source, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToArray();
    }

    private static JobMatchResultDto Score(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        IReadOnlyDictionary<string, CatalogSkillDefinition> skillIndex)
    {
        var reasons = new List<string>();
        var missingRequirements = new List<string>();
        var candidateSkillSlugs = profile.Skills.Select(x => x.SkillSlug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        decimal score = 0m;
        score += ScoreSkills(profile, job, rules, skillIndex, candidateSkillSlugs, reasons, missingRequirements);
        score += ScoreSeniority(profile, job, rules, reasons, missingRequirements);
        score += ScoreLocation(profile, job, rules, reasons, missingRequirements, out var locationFit);
        score += ScoreSalary(profile, job, rules, reasons, missingRequirements, out var salaryFit);
        score += ScoreLanguages(profile, job, rules, reasons, missingRequirements, out var languageFit);
        score -= CalculateQualityPenalty(job, rules, reasons);

        score = Math.Clamp(score, 0m, 100m);
        var matchBand = score >= 75m ? MatchBand.High : score >= 45m ? MatchBand.Medium : MatchBand.Low;

        if (reasons.Count == 0)
        {
            reasons.Add("El match se calculo con datos limitados.");
        }

        return new JobMatchResultDto(
            JobOfferDto.FromDomain(job),
            decimal.Round(score, 2),
            matchBand.ToString(),
            rules.Version,
            reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            missingRequirements.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            salaryFit,
            locationFit,
            languageFit);
    }

    private static decimal ScoreSkills(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        IReadOnlyDictionary<string, CatalogSkillDefinition> skillIndex,
        HashSet<string> candidateSkillSlugs,
        List<string> reasons,
        List<string> missingRequirements)
    {
        if (job.SkillTags.Count == 0)
        {
            reasons.Add("La vacante no tiene skills estructuradas; el score tecnico es parcial.");
            return 0m;
        }

        var requiredSkills = job.SkillTags.Where(x => x.IsRequired).ToArray();
        var optionalSkills = job.SkillTags.Where(x => !x.IsRequired).ToArray();

        decimal requiredAccumulator = 0m;
        decimal optionalAccumulator = 0m;
        var exactMatches = new List<string>();
        var relatedMatches = new List<string>();

        foreach (var requirement in requiredSkills)
        {
            var multiplier = ResolveSkillMultiplier(requirement.SkillSlug, candidateSkillSlugs, skillIndex, rules);
            requiredAccumulator += multiplier;

            if (multiplier >= 1m)
            {
                exactMatches.Add(requirement.SkillName);
            }
            else if (multiplier > 0)
            {
                relatedMatches.Add(requirement.SkillName);
                missingRequirements.Add($"Tu perfil tiene experiencia relacionada con {requirement.SkillName}, pero no coincidencia exacta.");
            }
            else
            {
                missingRequirements.Add($"Falta skill requerida: {requirement.SkillName}");
            }
        }

        foreach (var optional in optionalSkills)
        {
            var multiplier = ResolveSkillMultiplier(optional.SkillSlug, candidateSkillSlugs, skillIndex, rules);
            optionalAccumulator += multiplier;

            if (multiplier >= 1m)
            {
                exactMatches.Add(optional.SkillName);
            }
            else if (multiplier > 0)
            {
                relatedMatches.Add(optional.SkillName);
            }
        }

        if (exactMatches.Count > 0)
        {
            reasons.Add($"Coincidencias tecnicas fuertes: {string.Join(", ", exactMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}.");
        }

        if (relatedMatches.Count > 0)
        {
            reasons.Add($"Coincidencias parciales por stack relacionado: {string.Join(", ", relatedMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}.");
        }

        var requiredScore = requiredSkills.Length == 0 ? rules.Weights.RequiredSkills * 0.65m : requiredAccumulator / requiredSkills.Length * rules.Weights.RequiredSkills;
        var optionalScore = optionalSkills.Length == 0 ? rules.Weights.OptionalSkills * 0.4m : optionalAccumulator / optionalSkills.Length * rules.Weights.OptionalSkills;

        return requiredScore + optionalScore;
    }

    private static decimal ResolveSkillMultiplier(
        string requiredSkillSlug,
        HashSet<string> candidateSkillSlugs,
        IReadOnlyDictionary<string, CatalogSkillDefinition> skillIndex,
        MatchingRuleSetDto rules)
    {
        if (candidateSkillSlugs.Contains(requiredSkillSlug))
        {
            return 1m;
        }

        if (skillIndex.TryGetValue(requiredSkillSlug, out var targetSkill))
        {
            if (targetSkill.EquivalentSlugs.Any(candidateSkillSlugs.Contains))
            {
                return rules.Skills.EquivalentSkillMultiplier;
            }

            if (targetSkill.RelatedSlugs.Any(candidateSkillSlugs.Contains))
            {
                return rules.Skills.RelatedSkillMultiplier;
            }
        }

        foreach (var candidateSkill in candidateSkillSlugs)
        {
            if (!skillIndex.TryGetValue(candidateSkill, out var candidateDefinition))
            {
                continue;
            }

            if (candidateDefinition.EquivalentSlugs.Contains(requiredSkillSlug, StringComparer.OrdinalIgnoreCase))
            {
                return rules.Skills.EquivalentSkillMultiplier;
            }

            if (candidateDefinition.RelatedSlugs.Contains(requiredSkillSlug, StringComparer.OrdinalIgnoreCase))
            {
                return rules.Skills.RelatedSkillMultiplier;
            }
        }

        return 0m;
    }

    private static decimal ScoreSeniority(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> reasons,
        List<string> missingRequirements)
    {
        decimal score = 0m;

        if (profile.SeniorityLevel == job.SeniorityLevel && job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += rules.Weights.Seniority;
            reasons.Add($"Tu seniority coincide con la vacante ({job.SeniorityLevel}).");
        }
        else if (profile.SeniorityLevel >= job.SeniorityLevel && job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += rules.Weights.Seniority * 0.7m;
            reasons.Add("Tu seniority cubre o supera el nivel esperado.");
        }
        else if (job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            missingRequirements.Add($"La vacante apunta a un nivel {job.SeniorityLevel}.");
            score += rules.Weights.Seniority * 0.15m;
        }
        else
        {
            score += rules.Weights.Seniority * 0.35m;
        }

        var expectedYears = job.SeniorityLevel switch
        {
            SeniorityLevel.Junior => 1m,
            SeniorityLevel.Mid => 3m,
            SeniorityLevel.Senior => 5m,
            SeniorityLevel.Lead => 7m,
            SeniorityLevel.Executive => 10m,
            _ => 0m
        };

        if (expectedYears == 0m)
        {
            score += rules.Weights.ExperienceYears * 0.4m;
        }
        else if (profile.YearsOfExperience >= expectedYears)
        {
            score += rules.Weights.ExperienceYears;
        }
        else
        {
            var ratio = Math.Clamp(profile.YearsOfExperience / expectedYears, 0.2m, 1m);
            score += rules.Weights.ExperienceYears * ratio;
            missingRequirements.Add($"La vacante sugiere al menos {expectedYears:0.#} anos de experiencia.");
        }

        return score;
    }

    private static decimal ScoreLocation(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> reasons,
        List<string> missingRequirements,
        out string locationFit)
    {
        if (job.WorkMode == WorkMode.Remote)
        {
            if (profile.Preferences.AcceptRemote)
            {
                reasons.Add("La vacante es remota y tu perfil acepta trabajo remoto.");
                locationFit = "RemoteCompatible";
                return rules.Weights.LocationWorkMode;
            }

            locationFit = "RemoteRejected";
            missingRequirements.Add("Tu perfil no prioriza trabajo remoto.");
            return rules.Weights.LocationWorkMode * 0.15m;
        }

        var targetCities = profile.LocationPreference.GetTargetCities();
        var targetCountries = profile.LocationPreference.GetTargetCountries();

        var cityMatch = !string.IsNullOrWhiteSpace(job.City) && (
            string.Equals(job.City, profile.LocationPreference.CurrentCity, StringComparison.OrdinalIgnoreCase) ||
            targetCities.Contains(job.City, StringComparer.OrdinalIgnoreCase));

        var countryMatch = !string.IsNullOrWhiteSpace(job.CountryCode) && (
            string.Equals(job.CountryCode, profile.LocationPreference.CurrentCountryCode, StringComparison.OrdinalIgnoreCase) ||
            targetCountries.Contains(job.CountryCode, StringComparer.OrdinalIgnoreCase));

        if (cityMatch || countryMatch)
        {
            reasons.Add("La ubicacion de la vacante coincide con tu perfil.");
            locationFit = "Compatible";
            return rules.Weights.LocationWorkMode;
        }

        if (profile.LocationPreference.IsWillingToRelocate)
        {
            reasons.Add("La vacante no coincide con tu ubicacion actual, pero tu perfil acepta reubicacion.");
            locationFit = "RelocationPossible";
            return rules.Weights.LocationWorkMode * 0.75m;
        }

        locationFit = "Mismatch";
        missingRequirements.Add("La ubicacion presencial/hibrida no coincide con tu perfil.");
        return rules.Weights.LocationWorkMode * 0.1m;
    }

    private static decimal ScoreSalary(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> reasons,
        List<string> missingRequirements,
        out string salaryFit)
    {
        if (!profile.SalaryExpectation.MinAmount.HasValue)
        {
            salaryFit = "NoExpectation";
            return rules.Weights.Salary * 0.6m;
        }

        if (!job.SalaryMin.HasValue && !job.SalaryMax.HasValue)
        {
            salaryFit = "Unknown";
            reasons.Add("La vacante no publica salario; score salarial neutro.");
            return rules.Weights.Salary * 0.5m;
        }

        var expectedMin = profile.SalaryExpectation.MinAmount.Value;
        var jobMax = job.SalaryMax ?? job.SalaryMin ?? 0m;

        if (jobMax >= expectedMin)
        {
            salaryFit = "Good";
            reasons.Add("La vacante esta dentro o cerca de tu expectativa salarial.");
            return rules.Weights.Salary;
        }

        var ratio = jobMax <= 0 ? 0m : Math.Clamp(jobMax / expectedMin, 0m, 1m);
        salaryFit = "BelowExpectation";
        missingRequirements.Add("El salario publicado parece estar por debajo de tu expectativa.");
        return rules.Weights.Salary * ratio * 0.5m;
    }

    private static decimal ScoreLanguages(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> reasons,
        List<string> missingRequirements,
        out string languageFit)
    {
        if (job.LanguageRequirements.Count == 0)
        {
            languageFit = "NotRequired";
            return rules.Weights.Languages * 0.6m;
        }

        var candidateLanguages = profile.Languages.ToDictionary(x => x.LanguageCode, x => x.Proficiency, StringComparer.OrdinalIgnoreCase);
        decimal metScore = 0m;

        foreach (var requirement in job.LanguageRequirements)
        {
            if (candidateLanguages.TryGetValue(requirement.LanguageCode, out var proficiency) && proficiency >= requirement.MinimumProficiency)
            {
                metScore += 1m;
            }
            else if (candidateLanguages.TryGetValue(requirement.LanguageCode, out var lowerProficiency) && lowerProficiency != LanguageProficiency.Unknown)
            {
                metScore += 0.4m;
                missingRequirements.Add($"Tu nivel de {requirement.LanguageName} parece inferior al requerido ({requirement.MinimumProficiency}).");
            }
            else
            {
                missingRequirements.Add($"Idioma requerido: {requirement.LanguageName} {requirement.MinimumProficiency}.");
            }
        }

        var ratio = metScore / job.LanguageRequirements.Count;

        if (ratio >= 1m)
        {
            reasons.Add("Cumples los requisitos de idioma detectados.");
            languageFit = "Strong";
            return rules.Weights.Languages;
        }

        if (ratio > 0m)
        {
            reasons.Add("Cumples parcialmente los requisitos de idioma.");
            languageFit = "Partial";
            return rules.Weights.Languages * ratio;
        }

        languageFit = "Missing";
        return 0m;
    }

    private static decimal CalculateQualityPenalty(JobOffer job, MatchingRuleSetDto rules, List<string> reasons)
    {
        decimal penalty = 0m;

        if (job.QualityScore < rules.Penalties.LowQualityThreshold)
        {
            var ratio = 1m - (job.QualityScore / Math.Max(0.01m, rules.Penalties.LowQualityThreshold));
            penalty += rules.Penalties.MaxQualityPenalty * Math.Clamp(ratio, 0m, 1m);
            reasons.Add("La vacante fue penalizada por calidad baja de datos.");
        }

        if (job.SkillTags.Count == 0)
        {
            penalty += rules.Penalties.MissingSkillSignalsPenalty;
        }

        return penalty;
    }
}
