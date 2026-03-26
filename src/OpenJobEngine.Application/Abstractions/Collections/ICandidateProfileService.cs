using OpenJobEngine.Application.Profiles;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface ICandidateProfileService
{
    Task<CandidateProfileDto> CreateAsync(CandidateProfileUpsertRequest request, CancellationToken cancellationToken);

    Task<CandidateProfileDto?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken);

    Task<CandidateProfileDto?> UpdateAsync(Guid profileId, CandidateProfileUpsertRequest request, CancellationToken cancellationToken);

    Task<CandidateProfileDto?> UpdatePreferencesAsync(Guid profileId, CandidatePreferencesUpdateRequest request, CancellationToken cancellationToken);

    Task<CandidateProfileDto?> UpdateSkillsAsync(Guid profileId, IReadOnlyCollection<CandidateSkillInput> skills, CancellationToken cancellationToken);

    Task<CandidateProfileDto?> UpdateLanguagesAsync(Guid profileId, IReadOnlyCollection<CandidateLanguageInput> languages, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SavedSearchDto>> GetSavedSearchesAsync(Guid profileId, CancellationToken cancellationToken);

    Task<SavedSearchDto?> AddSavedSearchAsync(Guid profileId, SavedSearchCreateRequest request, CancellationToken cancellationToken);

    Task<ProfileAlertDto?> AddAlertAsync(Guid profileId, ProfileAlertCreateRequest request, CancellationToken cancellationToken);
}
