using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface INormalizationService
{
    JobOffer Normalize(RawJobOffer raw);
}
