using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.Entities;

public sealed class JobOfferLanguageRequirement
{
    private JobOfferLanguageRequirement()
    {
    }

    public JobOfferLanguageRequirement(
        Guid id,
        Guid jobOfferId,
        string languageCode,
        string languageName,
        LanguageProficiency minimumProficiency,
        bool isRequired,
        decimal confidenceScore)
    {
        Id = id;
        JobOfferId = jobOfferId;
        LanguageCode = languageCode;
        LanguageName = languageName;
        MinimumProficiency = minimumProficiency;
        IsRequired = isRequired;
        ConfidenceScore = confidenceScore;
    }

    public Guid Id { get; private set; }

    public Guid JobOfferId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string LanguageName { get; private set; } = string.Empty;

    public LanguageProficiency MinimumProficiency { get; private set; }

    public bool IsRequired { get; private set; }

    public decimal ConfidenceScore { get; private set; }

    public JobOfferLanguageRequirement CloneForJob(Guid jobOfferId)
    {
        return new JobOfferLanguageRequirement(
            Guid.NewGuid(),
            jobOfferId,
            LanguageCode,
            LanguageName,
            MinimumProficiency,
            IsRequired,
            ConfidenceScore);
    }
}
