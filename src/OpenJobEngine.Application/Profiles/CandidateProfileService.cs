using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Profiles;

public sealed class CandidateProfileService(
    ICandidateProfileRepository candidateProfileRepository,
    IUnitOfWork unitOfWork) : ICandidateProfileService
{
    public async Task<CandidateProfileDto> CreateAsync(CandidateProfileUpsertRequest request, CancellationToken cancellationToken)
    {
        var profile = BuildProfile(Guid.NewGuid(), request);
        await candidateProfileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CandidateProfileDto.FromDomain(profile);
    }

    public async Task<CandidateProfileDto?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        return profile is null ? null : CandidateProfileDto.FromDomain(profile);
    }

    public async Task<CandidateProfileDto?> UpdateAsync(Guid profileId, CandidateProfileUpsertRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        ApplyProfile(profile, request);
        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CandidateProfileDto.FromDomain(profile);
    }

    public async Task<CandidateProfileDto?> UpdatePreferencesAsync(Guid profileId, CandidatePreferencesUpdateRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        profile.UpdatePreferences(ParseWorkMode(request.PreferredWorkMode), request.AcceptRemote, request.AcceptHybrid, request.AcceptOnSite);
        profile.UpdateSalaryExpectation(request.SalaryMin, request.SalaryMax, request.SalaryCurrency);
        profile.UpdateLocationPreference(
            request.CurrentCity,
            request.CurrentRegion,
            request.CurrentCountryCode,
            request.TargetCities,
            request.TargetCountries,
            request.IsWillingToRelocate);

        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CandidateProfileDto.FromDomain(profile);
    }

    public async Task<CandidateProfileDto?> UpdateSkillsAsync(Guid profileId, IReadOnlyCollection<CandidateSkillInput> skills, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        profile.ReplaceSkills(MapSkills(profileId, skills));
        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CandidateProfileDto.FromDomain(profile);
    }

    public async Task<CandidateProfileDto?> UpdateLanguagesAsync(Guid profileId, IReadOnlyCollection<CandidateLanguageInput> languages, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        profile.ReplaceLanguages(MapLanguages(profileId, languages));
        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CandidateProfileDto.FromDomain(profile);
    }

    public async Task<IReadOnlyCollection<SavedSearchDto>> GetSavedSearchesAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        return profile?.SavedSearches.Select(SavedSearchDto.FromDomain).ToArray() ?? Array.Empty<SavedSearchDto>();
    }

    public async Task<SavedSearchDto?> AddSavedSearchAsync(Guid profileId, SavedSearchCreateRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var search = new SavedSearch(
            Guid.NewGuid(),
            profileId,
            request.Name.Trim(),
            request.Query,
            request.Location,
            request.RemoteOnly,
            request.MinimumSalary,
            request.MinimumMatchScore,
            request.Source,
            request.IsActive);

        profile.AddSavedSearch(search);
        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SavedSearchDto.FromDomain(search);
    }

    public async Task<ProfileAlertDto?> AddAlertAsync(Guid profileId, ProfileAlertCreateRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var alert = new ProfileAlert(
            Guid.NewGuid(),
            profileId,
            request.Name.Trim(),
            Enum.TryParse<AlertChannelType>(request.ChannelType, true, out var channelType) ? channelType : AlertChannelType.Passive,
            request.Target,
            request.MinimumMatchScore,
            request.IsActive);

        profile.AddAlert(alert);
        await candidateProfileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProfileAlertDto.FromDomain(alert);
    }

    private static CandidateProfile BuildProfile(Guid id, CandidateProfileUpsertRequest request)
    {
        var profile = new CandidateProfile(id, request.TargetTitle.Trim(), request.YearsOfExperience, ParseSeniority(request.SeniorityLevel));
        ApplyProfile(profile, request);
        return profile;
    }

    private static void ApplyProfile(CandidateProfile profile, CandidateProfileUpsertRequest request)
    {
        profile.UpdateBasics(request.TargetTitle, request.ProfessionalSummary, request.YearsOfExperience, ParseSeniority(request.SeniorityLevel));
        profile.UpdatePreferences(ParseWorkMode(request.PreferredWorkMode), request.AcceptRemote, request.AcceptHybrid, request.AcceptOnSite);
        profile.UpdateSalaryExpectation(request.SalaryMin, request.SalaryMax, request.SalaryCurrency);
        profile.UpdateLocationPreference(
            request.CurrentCity,
            request.CurrentRegion,
            request.CurrentCountryCode,
            request.TargetCities,
            request.TargetCountries,
            request.IsWillingToRelocate);
        profile.ReplaceSkills(MapSkills(profile.Id, request.Skills));
        profile.ReplaceLanguages(MapLanguages(profile.Id, request.Languages));
    }

    private static IReadOnlyCollection<CandidateSkill> MapSkills(Guid profileId, IReadOnlyCollection<CandidateSkillInput> inputs)
    {
        return inputs
            .Where(x => !string.IsNullOrWhiteSpace(x.SkillName))
            .Select(x => new CandidateSkill(
                Guid.NewGuid(),
                profileId,
                x.SkillName.Trim(),
                string.IsNullOrWhiteSpace(x.SkillSlug) ? x.SkillName.Trim().ToLowerInvariant() : x.SkillSlug.Trim().ToLowerInvariant(),
                ParseSkillCategory(x.Category),
                x.YearsExperience,
                Math.Clamp(x.ProficiencyScore, 1, 5)))
            .ToArray();
    }

    private static IReadOnlyCollection<CandidateLanguage> MapLanguages(Guid profileId, IReadOnlyCollection<CandidateLanguageInput> inputs)
    {
        return inputs
            .Where(x => !string.IsNullOrWhiteSpace(x.LanguageName))
            .Select(x => new CandidateLanguage(
                Guid.NewGuid(),
                profileId,
                string.IsNullOrWhiteSpace(x.LanguageCode) ? x.LanguageName.Trim().ToLowerInvariant() : x.LanguageCode.Trim().ToLowerInvariant(),
                x.LanguageName.Trim(),
                ParseLanguageProficiency(x.Proficiency)))
            .ToArray();
    }

    private static SeniorityLevel ParseSeniority(string value)
    {
        return Enum.TryParse<SeniorityLevel>(value, true, out var seniorityLevel)
            ? seniorityLevel
            : SeniorityLevel.Unknown;
    }

    private static WorkMode ParseWorkMode(string value)
    {
        return Enum.TryParse<WorkMode>(value, true, out var workMode)
            ? workMode
            : WorkMode.Unknown;
    }

    private static SkillCategory ParseSkillCategory(string value)
    {
        return Enum.TryParse<SkillCategory>(value, true, out var skillCategory)
            ? skillCategory
            : SkillCategory.Other;
    }

    private static LanguageProficiency ParseLanguageProficiency(string value)
    {
        return Enum.TryParse<LanguageProficiency>(value, true, out var proficiency)
            ? proficiency
            : LanguageProficiency.Unknown;
    }
}
