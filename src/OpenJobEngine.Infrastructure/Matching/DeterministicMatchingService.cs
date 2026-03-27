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
            ?? throw new ResourceNotFoundException($"Profile '{request.ProfileId}' was not found.");

        var jobs = await jobRepository.ListActiveAsync(cancellationToken);
        var filteredJobs = ApplyFilters(jobs, request);
        var rules = matchingRulesProvider.GetCurrent();
        var previousExecution = await matchExecutionRepository.GetLatestForProfileAsync(request.ProfileId, cancellationToken);
        var matches = filteredJobs
            .Select(job => Score(profile, job, rules, skillIndex))
            .Where(result => !request.MinimumMatchScore.HasValue || result.MatchScore >= request.MinimumMatchScore.Value)
            .OrderByDescending(result => result.MatchScore)
            .ThenByDescending(result => result.Job.PublishedAtUtc ?? result.Job.CollectedAtUtc)
            .ToArray();
        var result = CreateSearchResult(request.ProfileId, rules.Version, matches, request.Page, request.PageSize);

        await PersistExecutionAsync(
            request.ProfileId,
            request.Query,
            request.MinimumMatchScore,
            matches,
            result.Results.Items,
            rules,
            previousExecution,
            cancellationToken);

        return result;
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

    public async Task<MatchingSearchResultDto> GetNewHighPriorityMatchesAsync(
        Guid profileId,
        decimal? minimumMatchScore,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Profile '{profileId}' was not found.");

        var rules = matchingRulesProvider.GetCurrent();
        var previousExecution = await matchExecutionRepository.GetLatestForProfileAsync(profileId, cancellationToken);
        var threshold = minimumMatchScore ?? rules.Tolerances.NewHighPriorityThreshold;
        var baselineUtc = previousExecution?.CreatedAtUtc;

        var matches = (await jobRepository.ListActiveAsync(cancellationToken))
            .Where(job => IsNewSinceBaseline(job, baselineUtc))
            .Select(job => Score(profile, job, rules, skillIndex))
            .Where(result => result.MatchScore >= threshold)
            .OrderByDescending(result => result.MatchScore)
            .ThenByDescending(result => result.Job.PublishedAtUtc ?? result.Job.CollectedAtUtc)
            .ToArray();

        var result = CreateSearchResult(profileId, rules.Version, matches, page, pageSize);

        await PersistExecutionAsync(
            profileId,
            "__new_high_priority__",
            threshold,
            matches,
            result.Results.Items,
            rules,
            previousExecution,
            cancellationToken);

        return result;
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

    private async Task PersistExecutionAsync(
        Guid profileId,
        string? query,
        decimal? minimumRequestedScore,
        IReadOnlyCollection<JobMatchResultDto> allMatches,
        IReadOnlyCollection<JobMatchResultDto> pagedMatches,
        MatchingRuleSetDto rules,
        MatchExecution? previousExecution,
        CancellationToken cancellationToken)
    {
        decimal? averageScore = allMatches.Count == 0 ? null : allMatches.Average(x => x.MatchScore);
        var highMatchCount = allMatches.Count(x => string.Equals(x.MatchBand, MatchBand.High.ToString(), StringComparison.OrdinalIgnoreCase));
        var mediumMatchCount = allMatches.Count(x => string.Equals(x.MatchBand, MatchBand.Medium.ToString(), StringComparison.OrdinalIgnoreCase));
        var lowMatchCount = allMatches.Count(x => string.Equals(x.MatchBand, MatchBand.Low.ToString(), StringComparison.OrdinalIgnoreCase));
        var strongMatchCount = allMatches.Count(x => x.StrongMatches.Count > 0);
        var partialMatchCount = allMatches.Count(x => x.PartialMatches.Count > 0);
        var hardFailureCount = allMatches.Count(x => x.HardFailures.Count > 0);
        var newHighPriorityCount = allMatches.Count(x =>
            x.MatchScore >= rules.Tolerances.NewHighPriorityThreshold &&
            IsNewSinceBaseline(x.Job, previousExecution?.CreatedAtUtc));

        await matchExecutionRepository.AddAsync(
            new MatchExecution(
                Guid.NewGuid(),
                profileId,
                query,
                pagedMatches.Count,
                allMatches.Count,
                pagedMatches.FirstOrDefault()?.MatchScore,
                averageScore,
                highMatchCount,
                mediumMatchCount,
                lowMatchCount,
                strongMatchCount,
                partialMatchCount,
                hardFailureCount,
                newHighPriorityCount,
                minimumRequestedScore,
                rules.Version),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static MatchingSearchResultDto CreateSearchResult(
        Guid profileId,
        string ruleVersion,
        IReadOnlyCollection<JobMatchResultDto> matches,
        int page,
        int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => pageSize
        };

        var pagedItems = matches
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        return new MatchingSearchResultDto(
            profileId,
            ruleVersion,
            new PagedResult<JobMatchResultDto>(pagedItems, normalizedPage, normalizedPageSize, matches.Count));
    }

    private static bool IsNewSinceBaseline(JobOfferDto job, DateTimeOffset? baselineUtc)
    {
        if (!baselineUtc.HasValue)
        {
            return true;
        }

        var referenceDate = job.PublishedAtUtc ?? job.CollectedAtUtc;
        return referenceDate > baselineUtc.Value;
    }

    private static bool IsNewSinceBaseline(JobOffer job, DateTimeOffset? baselineUtc)
    {
        if (!baselineUtc.HasValue)
        {
            return true;
        }

        var referenceDate = job.PublishedAtUtc ?? job.LastSeenAtUtc;
        return referenceDate > baselineUtc.Value;
    }

    private static JobMatchResultDto Score(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        IReadOnlyDictionary<string, CatalogSkillDefinition> skillIndex)
    {
        var strongMatches = new List<string>();
        var partialMatches = new List<string>();
        var hardFailures = new List<string>();
        var missingRequirements = new List<string>();
        var candidateSkillSlugs = profile.Skills.Select(x => x.SkillSlug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        decimal score = 0m;
        score += ScoreSkills(profile, job, rules, skillIndex, candidateSkillSlugs, strongMatches, partialMatches, hardFailures, missingRequirements);
        score += ScoreSeniority(profile, job, rules, strongMatches, partialMatches, missingRequirements);
        score += ScoreLocation(profile, job, rules, strongMatches, partialMatches, hardFailures, missingRequirements, out var locationFit);
        score += ScoreSalary(profile, job, rules, strongMatches, partialMatches, hardFailures, missingRequirements, out var salaryFit);
        score += ScoreLanguages(profile, job, rules, strongMatches, partialMatches, hardFailures, missingRequirements, out var languageFit);
        score += ScoreCompanyPreferences(profile, job, rules, strongMatches, hardFailures);
        score -= CalculateQualityPenalty(job, rules, partialMatches);
        score -= CalculateHardFailurePenalty(hardFailures.Count, rules);

        score = Math.Clamp(score, 0m, 100m);
        var matchBand = score >= 75m ? MatchBand.High : score >= 45m ? MatchBand.Medium : MatchBand.Low;
        var matchReasons = strongMatches.Concat(partialMatches).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var missingItems = missingRequirements.Concat(hardFailures).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (matchReasons.Length == 0)
        {
            partialMatches.Add("El match se calculo con datos limitados.");
            matchReasons = strongMatches.Concat(partialMatches).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return new JobMatchResultDto(
            JobOfferDto.FromDomain(job),
            decimal.Round(score, 2),
            matchBand.ToString(),
            rules.Version,
            strongMatches.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            partialMatches.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            hardFailures.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            matchReasons,
            missingItems,
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
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> hardFailures,
        List<string> missingRequirements)
    {
        if (job.SkillTags.Count == 0)
        {
            partialMatches.Add("La vacante no tiene skills estructuradas; el score tecnico es parcial.");
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
            strongMatches.Add($"Coincidencias tecnicas fuertes: {string.Join(", ", exactMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}.");
        }

        if (relatedMatches.Count > 0)
        {
            partialMatches.Add($"Coincidencias parciales por stack relacionado: {string.Join(", ", relatedMatches.Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}.");
        }

        var requiredCoverage = requiredSkills.Length == 0 ? 1m : requiredAccumulator / requiredSkills.Length;
        if (requiredSkills.Length > 0 && requiredCoverage < rules.HardRequirements.MinimumRequiredSkillsCoverage)
        {
            hardFailures.Add("No cubres suficientes skills requeridas para esta vacante.");
        }

        var requiredScore = requiredSkills.Length == 0 ? rules.Weights.RequiredSkills * 0.65m : requiredCoverage * rules.Weights.RequiredSkills;
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
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> missingRequirements)
    {
        decimal score = 0m;

        if (profile.SeniorityLevel == job.SeniorityLevel && job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += rules.Weights.Seniority;
            strongMatches.Add($"Tu seniority coincide con la vacante ({job.SeniorityLevel}).");
        }
        else if (profile.SeniorityLevel >= job.SeniorityLevel && job.SeniorityLevel != SeniorityLevel.Unknown)
        {
            score += rules.Weights.Seniority * 0.7m;
            partialMatches.Add("Tu seniority cubre o supera el nivel esperado.");
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
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> hardFailures,
        List<string> missingRequirements,
        out string locationFit)
    {
        var excludedWorkModes = profile.Preferences
            .GetExcludedWorkModes()
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (excludedWorkModes.Contains(job.WorkMode.ToString()))
        {
            hardFailures.Add($"Tu perfil excluye vacantes con modalidad {job.WorkMode}.");
        }

        if (job.WorkMode == WorkMode.Remote)
        {
            if (profile.Preferences.AcceptRemote)
            {
                strongMatches.Add("La vacante es remota y tu perfil acepta trabajo remoto.");
                locationFit = "RemoteCompatible";
                return rules.Weights.LocationWorkMode;
            }

            locationFit = "RemoteRejected";
            missingRequirements.Add("Tu perfil no prioriza trabajo remoto.");
            hardFailures.Add("La vacante es remota, pero tu perfil no acepta este modo como preferencia.");
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
            strongMatches.Add("La ubicacion de la vacante coincide con tu perfil.");
            locationFit = "Compatible";
            return ApplyTimezonePreferenceBoost(profile, job, rules, rules.Weights.LocationWorkMode, strongMatches, partialMatches, hardFailures);
        }

        if (profile.LocationPreference.IsWillingToRelocate)
        {
            partialMatches.Add("La vacante no coincide con tu ubicacion actual, pero tu perfil acepta reubicacion.");
            locationFit = "RelocationPossible";
            return ApplyTimezonePreferenceBoost(profile, job, rules, rules.Weights.LocationWorkMode * 0.75m, strongMatches, partialMatches, hardFailures);
        }

        locationFit = "Mismatch";
        missingRequirements.Add("La ubicacion presencial/hibrida no coincide con tu perfil.");
        return rules.Weights.LocationWorkMode * 0.1m;
    }

    private static decimal ScoreSalary(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> hardFailures,
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
            partialMatches.Add("La vacante no publica salario; score salarial neutro.");
            return rules.Weights.Salary * 0.5m;
        }

        var expectedMin = profile.SalaryExpectation.MinAmount.Value;
        var jobMax = job.SalaryMax ?? job.SalaryMin ?? 0m;

        if (jobMax >= expectedMin)
        {
            salaryFit = "Good";
            strongMatches.Add("La vacante esta dentro o cerca de tu expectativa salarial.");
            return rules.Weights.Salary;
        }

        var ratio = jobMax <= 0 ? 0m : Math.Clamp(jobMax / expectedMin, 0m, 1m);
        salaryFit = "BelowExpectation";
        missingRequirements.Add("El salario publicado parece estar por debajo de tu expectativa.");
        if (ratio >= rules.Tolerances.SalaryCloseMatchRatio)
        {
            partialMatches.Add("El salario esta ligeramente por debajo de tu expectativa, pero dentro de la tolerancia configurada.");
            return rules.Weights.Salary * ratio * 0.75m;
        }

        hardFailures.Add("La vacante queda materialmente por debajo de tu expectativa salarial.");
        return rules.Weights.Salary * ratio * 0.35m;
    }

    private static decimal ScoreLanguages(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> hardFailures,
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
            strongMatches.Add("Cumples los requisitos de idioma detectados.");
            languageFit = "Strong";
            return rules.Weights.Languages;
        }

        if (ratio > 0m)
        {
            partialMatches.Add("Cumples parcialmente los requisitos de idioma.");
            languageFit = "Partial";
            if (ratio < rules.HardRequirements.MinimumLanguageCoverage)
            {
                hardFailures.Add("El nivel de idioma requerido no alcanza la cobertura minima configurada.");
            }

            return rules.Weights.Languages * ratio;
        }

        languageFit = "Missing";
        hardFailures.Add("No cumples los requisitos de idioma detectados.");
        return 0m;
    }

    private static decimal CalculateQualityPenalty(JobOffer job, MatchingRuleSetDto rules, List<string> partialMatches)
    {
        decimal penalty = 0m;

        if (job.QualityScore < rules.Penalties.LowQualityThreshold)
        {
            var ratio = 1m - (job.QualityScore / Math.Max(0.01m, rules.Penalties.LowQualityThreshold));
            penalty += rules.Penalties.MaxQualityPenalty * Math.Clamp(ratio, 0m, 1m);
            partialMatches.Add("La vacante fue penalizada por calidad baja de datos.");
        }

        if (job.SkillTags.Count == 0)
        {
            penalty += rules.Penalties.MissingSkillSignalsPenalty;
        }

        return penalty;
    }

    private static decimal ScoreCompanyPreferences(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        List<string> strongMatches,
        List<string> hardFailures)
    {
        var includedKeywords = profile.Preferences.GetIncludedCompanyKeywords();
        var excludedKeywords = profile.Preferences.GetExcludedCompanyKeywords();
        var companyText = $"{job.CompanyName} {job.Title} {job.Description}".Trim();

        if (excludedKeywords.Any(keyword => companyText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            hardFailures.Add("La vacante coincide con keywords de empresa excluidas por tu perfil.");
            return -rules.Penalties.CompanyKeywordExclusionPenalty;
        }

        if (includedKeywords.Any(keyword => companyText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            strongMatches.Add("La vacante coincide con preferencias de empresa/industria de tu perfil.");
            return rules.Tolerances.CompanyKeywordInclusionBoost;
        }

        return 0m;
    }

    private static decimal ApplyTimezonePreferenceBoost(
        CandidateProfile profile,
        JobOffer job,
        MatchingRuleSetDto rules,
        decimal baseScore,
        List<string> strongMatches,
        List<string> partialMatches,
        List<string> hardFailures)
    {
        var targetTimezones = profile.LocationPreference.GetTargetTimezones();
        if (targetTimezones.Count == 0 || string.IsNullOrWhiteSpace(job.TimeZone))
        {
            return baseScore;
        }

        if (targetTimezones.Contains(job.TimeZone, StringComparer.OrdinalIgnoreCase))
        {
            strongMatches.Add("La vacante coincide con una zona horaria objetivo de tu perfil.");
            return baseScore + rules.Tolerances.TimezoneMatchBoost;
        }

        partialMatches.Add("La vacante no coincide con tus zonas horarias objetivo.");
        hardFailures.Add("La zona horaria de la vacante no coincide con tu preferencia operativa.");
        return Math.Max(0m, baseScore - rules.Penalties.TimezoneMismatchPenalty);
    }

    private static decimal CalculateHardFailurePenalty(int hardFailureCount, MatchingRuleSetDto rules)
    {
        return hardFailureCount <= 0 ? 0m : hardFailureCount * rules.Penalties.HardFailurePenalty;
    }
}
