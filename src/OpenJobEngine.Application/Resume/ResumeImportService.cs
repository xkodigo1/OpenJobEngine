using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Profiles;

namespace OpenJobEngine.Application.Resume;

public sealed class ResumeImportService(
    ICandidateProfileRepository candidateProfileRepository,
    ICandidateProfileService candidateProfileService,
    IResumeTextExtractor resumeTextExtractor,
    IResumeProfileExtractor resumeProfileExtractor) : IResumeImportService
{
    public async Task<ResumeImportPreviewDto?> ImportAsync(ResumeImportRequest request, CancellationToken cancellationToken)
    {
        var profile = await candidateProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var extractedText = await resumeTextExtractor.ExtractTextAsync(request.Content, request.FileName, cancellationToken);
        var extractedProfile = resumeProfileExtractor.Extract(extractedText);
        var warnings = new List<string>();
        var shouldApply = request.ApplyToProfile && !string.IsNullOrWhiteSpace(extractedText);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            warnings.Add("No se pudo extraer texto util del PDF.");
        }

        if (request.ApplyToProfile && !shouldApply)
        {
            warnings.Add("No se aplicaron cambios porque no se extrajo texto util.");
        }

        if (shouldApply)
        {
            await candidateProfileService.UpdateAsync(request.ProfileId, extractedProfile.SuggestedProfile, cancellationToken);
        }

        return new ResumeImportPreviewDto(
            request.ProfileId,
            request.FileName,
            extractedText.Length > 1200 ? extractedText[..1200] : extractedText,
            extractedProfile,
            warnings.Concat(extractedProfile.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            shouldApply);
    }
}
