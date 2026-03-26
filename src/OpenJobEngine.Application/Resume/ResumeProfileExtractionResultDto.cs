using OpenJobEngine.Application.Profiles;

namespace OpenJobEngine.Application.Resume;

public sealed record ResumeProfileExtractionResultDto(
    CandidateProfileUpsertRequest SuggestedProfile,
    IReadOnlyDictionary<string, decimal> FieldConfidences,
    IReadOnlyCollection<string> Warnings,
    IReadOnlyDictionary<string, string> DetectedSections,
    IReadOnlyCollection<string> DetectedSkills,
    IReadOnlyCollection<string> DetectedLanguages,
    decimal? EstimatedYearsOfExperience);
