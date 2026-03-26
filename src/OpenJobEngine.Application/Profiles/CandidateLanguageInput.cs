namespace OpenJobEngine.Application.Profiles;

public sealed record CandidateLanguageInput(
    string LanguageCode,
    string LanguageName,
    string Proficiency);
