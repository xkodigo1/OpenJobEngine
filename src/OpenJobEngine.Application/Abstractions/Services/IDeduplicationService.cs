using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IDeduplicationService
{
    string BuildKey(RawJobOffer raw);
}
