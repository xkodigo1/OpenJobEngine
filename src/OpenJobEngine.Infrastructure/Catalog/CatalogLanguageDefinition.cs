namespace OpenJobEngine.Infrastructure.Catalog;

public sealed record CatalogLanguageDefinition(
    string Code,
    string Name,
    IReadOnlyCollection<string> Tokens);
