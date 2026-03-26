namespace OpenJobEngine.Infrastructure.Options;

public sealed class ProviderCatalogOptions
{
    public ComputrabajoProviderOptions Computrabajo { get; init; } = new();

    public AdzunaProviderOptions Adzuna { get; init; } = new();
}
