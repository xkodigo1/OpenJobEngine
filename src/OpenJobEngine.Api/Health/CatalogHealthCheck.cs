using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenJobEngine.Infrastructure.Catalog;

namespace OpenJobEngine.Api.Health;

internal sealed class CatalogHealthCheck(ITechnologyTaxonomyProvider taxonomyProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var skills = taxonomyProvider.GetSkills();
            var languages = taxonomyProvider.GetLanguages();
            var locations = taxonomyProvider.GetLocations();

            if (skills.Count == 0 || languages.Count == 0 || locations.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("One or more catalog collections are empty."));
            }

            IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
            {
                ["skills"] = skills.Count,
                ["languages"] = languages.Count,
                ["locations"] = locations.Count
            };

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Catalogs loaded successfully.",
                    data));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Catalog loading failed.", exception));
        }
    }
}
