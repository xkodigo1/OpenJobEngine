using OpenJobEngine.Application.Profiles;

namespace OpenJobEngine.Application.Resume;

public sealed record ResumeImportPreviewDto(
    Guid ProfileId,
    string FileName,
    string TextPreview,
    ResumeProfileExtractionResultDto SuggestedImport,
    IReadOnlyCollection<string> Warnings,
    bool AppliedToProfile);
