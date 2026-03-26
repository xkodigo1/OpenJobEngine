using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Api.Contracts;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Matching;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;

namespace OpenJobEngine.Api.Controllers;

[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController(
    ICandidateProfileService candidateProfileService,
    IResumeImportService resumeImportService,
    IMatchingService matchingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CandidateProfileDto>> CreateProfile(
        [FromBody] CandidateProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProfile), new { profileId = profile.Id }, profile);
    }

    [HttpGet("{profileId:guid}")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> GetProfile(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.GetByIdAsync(profileId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("{profileId:guid}")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateProfile(
        Guid profileId,
        [FromBody] CandidateProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateAsync(profileId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPatch("{profileId:guid}/preferences")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdatePreferences(
        Guid profileId,
        [FromBody] CandidatePreferencesUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdatePreferencesAsync(profileId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPatch("{profileId:guid}/skills")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateSkills(
        Guid profileId,
        [FromBody] IReadOnlyCollection<CandidateSkillInput> request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateSkillsAsync(profileId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPatch("{profileId:guid}/languages")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateLanguages(
        Guid profileId,
        [FromBody] IReadOnlyCollection<CandidateLanguageInput> request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateLanguagesAsync(profileId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("{profileId:guid}/resume")]
    [ProducesResponseType(typeof(ResumeImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeImportPreviewDto>> ImportResume(
        Guid profileId,
        [FromForm] ResumeUploadRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var preview = await resumeImportService.ImportAsync(
            new ResumeImportRequest(
                profileId,
                request.File.FileName,
                request.File.ContentType,
                memoryStream.ToArray(),
                request.ApplyToProfile),
            cancellationToken);

        return preview is null ? NotFound() : Ok(preview);
    }

    [HttpGet("{profileId:guid}/matches")]
    [ProducesResponseType(typeof(MatchingSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchingSearchResultDto>> GetMatches(
        Guid profileId,
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] bool? remoteOnly,
        [FromQuery] decimal? salaryMin,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] decimal? minimumMatchScore = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await matchingService.SearchAsync(
            new MatchingSearchRequest(profileId, query, location, remoteOnly, salaryMin, source, page, pageSize, minimumMatchScore),
            cancellationToken));
    }

    [HttpGet("{profileId:guid}/saved-searches")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SavedSearchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SavedSearchDto>>> GetSavedSearches(Guid profileId, CancellationToken cancellationToken)
    {
        return Ok(await candidateProfileService.GetSavedSearchesAsync(profileId, cancellationToken));
    }

    [HttpPost("{profileId:guid}/saved-searches")]
    [ProducesResponseType(typeof(SavedSearchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedSearchDto>> AddSavedSearch(
        Guid profileId,
        [FromBody] SavedSearchCreateRequest request,
        CancellationToken cancellationToken)
    {
        var savedSearch = await candidateProfileService.AddSavedSearchAsync(profileId, request, cancellationToken);
        return savedSearch is null ? NotFound() : Ok(savedSearch);
    }

    [HttpPost("{profileId:guid}/alerts")]
    [ProducesResponseType(typeof(ProfileAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileAlertDto>> AddAlert(
        Guid profileId,
        [FromBody] ProfileAlertCreateRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await candidateProfileService.AddAlertAsync(profileId, request, cancellationToken);
        return alert is null ? NotFound() : Ok(alert);
    }
}
