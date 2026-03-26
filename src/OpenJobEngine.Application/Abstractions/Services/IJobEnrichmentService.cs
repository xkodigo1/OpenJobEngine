using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IJobEnrichmentService
{
    JobOffer Enrich(JobOffer jobOffer, RawJobOffer rawJobOffer);
}
