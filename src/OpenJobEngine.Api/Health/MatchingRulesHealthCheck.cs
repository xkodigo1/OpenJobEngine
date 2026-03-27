using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenJobEngine.Application.Abstractions.Services;

namespace OpenJobEngine.Api.Health;

internal sealed class MatchingRulesHealthCheck(IMatchingRulesProvider matchingRulesProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = matchingRulesProvider.GetCurrent();

            if (string.IsNullOrWhiteSpace(rules.Version))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Matching rules are missing a version."));
            }

            IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
            {
                ["version"] = rules.Version,
                ["requiredSkillsWeight"] = rules.Weights.RequiredSkills,
                ["optionalSkillsWeight"] = rules.Weights.OptionalSkills
            };

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Matching rules loaded successfully.",
                    data));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Matching rules loading failed.", exception));
        }
    }
}
