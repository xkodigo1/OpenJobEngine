namespace OpenJobEngine.Infrastructure.Catalog;

public interface ITechnologyTaxonomyProvider
{
    IReadOnlyCollection<CatalogSkillDefinition> GetSkills();

    IReadOnlyCollection<CatalogLanguageDefinition> GetLanguages();

    IReadOnlyCollection<CatalogLocationDefinition> GetLocations();
}
