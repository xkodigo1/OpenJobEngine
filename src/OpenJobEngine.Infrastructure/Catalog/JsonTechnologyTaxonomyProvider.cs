using System.Text.Json;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Catalog;

public sealed class JsonTechnologyTaxonomyProvider : ITechnologyTaxonomyProvider
{
    private readonly Lazy<CatalogSnapshot> snapshot;

    public JsonTechnologyTaxonomyProvider()
    {
        snapshot = new Lazy<CatalogSnapshot>(LoadSnapshot, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyCollection<CatalogSkillDefinition> GetSkills() => snapshot.Value.Skills;

    public IReadOnlyCollection<CatalogLanguageDefinition> GetLanguages() => snapshot.Value.Languages;

    public IReadOnlyCollection<CatalogLocationDefinition> GetLocations() => snapshot.Value.Locations;

    private static CatalogSnapshot LoadSnapshot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var dataDirectory = Path.Combine(baseDirectory, "Catalog", "Data");

        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"Catalog data directory was not found: {dataDirectory}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var skills = JsonSerializer.Deserialize<List<SkillRecord>>(
                         File.ReadAllText(Path.Combine(dataDirectory, "skills.json")),
                         options)
                     ?? [];

        var languages = JsonSerializer.Deserialize<List<LanguageRecord>>(
                            File.ReadAllText(Path.Combine(dataDirectory, "languages.json")),
                            options)
                        ?? [];

        var locations = JsonSerializer.Deserialize<List<LocationRecord>>(
                            File.ReadAllText(Path.Combine(dataDirectory, "locations.json")),
                            options)
                        ?? [];

        return new CatalogSnapshot(
            skills.Select(x => new CatalogSkillDefinition(
                    x.Name,
                    x.Slug,
                    Enum.TryParse<SkillCategory>(x.Category, true, out var category) ? category : SkillCategory.Other,
                    x.Tokens,
                    x.Aliases ?? [],
                    x.Equivalents ?? [],
                    x.Related ?? []))
                .ToArray(),
            languages.Select(x => new CatalogLanguageDefinition(x.Code, x.Name, x.Tokens)).ToArray(),
            locations.Select(x => new CatalogLocationDefinition(x.Key, x.City, x.Region, x.CountryCode, x.TimeZone, x.Aliases)).ToArray());
    }

    private sealed record CatalogSnapshot(
        IReadOnlyCollection<CatalogSkillDefinition> Skills,
        IReadOnlyCollection<CatalogLanguageDefinition> Languages,
        IReadOnlyCollection<CatalogLocationDefinition> Locations);

    private sealed record SkillRecord(
        string Name,
        string Slug,
        string Category,
        IReadOnlyCollection<string> Tokens,
        IReadOnlyCollection<string>? Aliases,
        IReadOnlyCollection<string>? Equivalents,
        IReadOnlyCollection<string>? Related);

    private sealed record LanguageRecord(string Code, string Name, IReadOnlyCollection<string> Tokens);

    private sealed record LocationRecord(string Key, string City, string? Region, string CountryCode, string? TimeZone, IReadOnlyCollection<string> Aliases);
}
