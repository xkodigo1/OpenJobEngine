using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.Services;

public sealed class DefaultDeduplicationService : IDeduplicationService
{
    public string BuildKey(RawJobOffer raw)
    {
        var title = TextCanonicalizer.CanonicalizeKeyPart(raw.Title);
        var company = TextCanonicalizer.CanonicalizeKeyPart(raw.CompanyName);
        var location = TextCanonicalizer.CanonicalizeKeyPart(raw.LocationText);

        return $"{title}|{company}|{location}";
    }
}
