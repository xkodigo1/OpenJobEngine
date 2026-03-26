using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Catalog;

public sealed record CatalogSkillDefinition(
    string Name,
    string Slug,
    SkillCategory Category,
    IReadOnlyCollection<string> Tokens,
    IReadOnlyCollection<string> Aliases,
    IReadOnlyCollection<string> EquivalentSlugs,
    IReadOnlyCollection<string> RelatedSlugs);
