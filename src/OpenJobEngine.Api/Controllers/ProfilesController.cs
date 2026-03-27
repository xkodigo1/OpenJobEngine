using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenJobEngine.Api.Contracts;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Matching;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;

namespace OpenJobEngine.Api.Controllers;

/// <summary>
/// Manages candidate profiles, resume imports, saved searches, alerts and profile-oriented match queries.
/// </summary>
[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController(
    ICandidateProfileService candidateProfileService,
    IResumeImportService resumeImportService,
    IMatchingService matchingService) : ControllerBase
{
    /// <summary>
    /// Creates a new candidate profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CandidateProfileDto>> CreateProfile(
        [FromBody] CandidateProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProfile), new { profileId = profile.Id }, profile);
    }

    /// <summary>
    /// Retrieves a candidate profile by identifier.
    /// </summary>
    [HttpGet("{profileId:guid}")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> GetProfile(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.GetByIdAsync(profileId, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(profile);
    }

    /// <summary>
    /// Replaces the current state of a candidate profile.
    /// </summary>
    [HttpPut("{profileId:guid}")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateProfile(
        Guid profileId,
        [FromBody] CandidateProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateAsync(profileId, request, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(profile);
    }

    /// <summary>
    /// Updates profile work mode, salary and location preferences.
    /// </summary>
    [HttpPatch("{profileId:guid}/preferences")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdatePreferences(
        Guid profileId,
        [FromBody] CandidatePreferencesUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdatePreferencesAsync(profileId, request, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(profile);
    }

    /// <summary>
    /// Replaces the skill collection of a candidate profile.
    /// </summary>
    [HttpPatch("{profileId:guid}/skills")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateSkills(
        Guid profileId,
        [FromBody] IReadOnlyCollection<CandidateSkillInput> request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateSkillsAsync(profileId, request, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(profile);
    }

    /// <summary>
    /// Replaces the language collection of a candidate profile.
    /// </summary>
    [HttpPatch("{profileId:guid}/languages")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateLanguages(
        Guid profileId,
        [FromBody] IReadOnlyCollection<CandidateLanguageInput> request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateLanguagesAsync(profileId, request, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(profile);
    }

    /// <summary>
    /// Imports a PDF resume, extracts a suggested profile and optionally applies it to the profile.
    /// </summary>
    [HttpPost("{profileId:guid}/resume")]
    [ProducesResponseType(typeof(ResumeImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("resume-import")]
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

        return preview is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(preview);
    }

    /// <summary>
    /// Searches matches using a stored profile and optional query filters.
    /// </summary>
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

    /// <summary>
    /// Returns newly discovered high-priority matches since the previous matching execution for the profile.
    /// </summary>
    [HttpGet("{profileId:guid}/matches/new-high-priority")]
    [ProducesResponseType(typeof(MatchingSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchingSearchResultDto>> GetNewHighPriorityMatches(
        Guid profileId,
        [FromQuery] decimal? minimumMatchScore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await matchingService.GetNewHighPriorityMatchesAsync(profileId, minimumMatchScore, page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Lists the saved searches of a candidate profile.
    /// </summary>
    [HttpGet("{profileId:guid}/saved-searches")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SavedSearchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SavedSearchDto>>> GetSavedSearches(Guid profileId, CancellationToken cancellationToken)
    {
        return Ok(await candidateProfileService.GetSavedSearchesAsync(profileId, cancellationToken));
    }

    /// <summary>
    /// Adds a saved search to a candidate profile.
    /// </summary>
    [HttpPost("{profileId:guid}/saved-searches")]
    [ProducesResponseType(typeof(SavedSearchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedSearchDto>> AddSavedSearch(
        Guid profileId,
        [FromBody] SavedSearchCreateRequest request,
        CancellationToken cancellationToken)
    {
        var savedSearch = await candidateProfileService.AddSavedSearchAsync(profileId, request, cancellationToken);
        return savedSearch is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(savedSearch);
    }

    /// <summary>
    /// Adds an alert configuration to a candidate profile.
    /// </summary>
    [HttpPost("{profileId:guid}/alerts")]
    [ProducesResponseType(typeof(ProfileAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileAlertDto>> AddAlert(
        Guid profileId,
        [FromBody] ProfileAlertCreateRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await candidateProfileService.AddAlertAsync(profileId, request, cancellationToken);
        return alert is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: $"Candidate profile '{profileId:D}' was not found.")
            : Ok(alert);
    }
}
