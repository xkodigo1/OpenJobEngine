namespace OpenJobEngine.Domain.Entities;

public sealed record RawJobOffer(
    string SourceName,
    string SourceJobId,
    string Title,
    string CompanyName,
    string? Description,
    string? LocationText,
    string? SalaryText,
    string Url,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyDictionary<string, string> Metadata);
