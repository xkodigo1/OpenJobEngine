namespace OpenJobEngine.Infrastructure.Catalog;

public sealed record CatalogLocationDefinition(
    string Key,
    string City,
    string? Region,
    string CountryCode,
    string? TimeZone,
    IReadOnlyCollection<string> Aliases);
