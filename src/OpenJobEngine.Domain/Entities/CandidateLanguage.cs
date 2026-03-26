using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class CandidateLanguage
{
    private CandidateLanguage()
    {
    }

    public CandidateLanguage(Guid id, Guid candidateProfileId, string languageCode, string languageName, LanguageProficiency proficiency)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        LanguageCode = languageCode;
        LanguageName = languageName;
        Proficiency = proficiency;
    }

    public Guid Id { get; private set; }

    public Guid CandidateProfileId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string LanguageName { get; private set; } = string.Empty;

    public LanguageProficiency Proficiency { get; private set; }

    public CandidateLanguage CloneForProfile(Guid candidateProfileId)
    {
        return new CandidateLanguage(Guid.NewGuid(), candidateProfileId, LanguageCode, LanguageName, Proficiency);
    }
}
