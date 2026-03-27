using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Infrastructure.Options;

namespace OpenJobEngine.Infrastructure.Providers;

public sealed class ConfiguredProviderLifecyclePolicyProvider(
    ComputrabajoProviderOptions computrabajoOptions,
    AdzunaProviderOptions adzunaOptions,
    GreenhouseProviderOptions greenhouseOptions,
    LeverProviderOptions leverOptions) : IProviderLifecyclePolicyProvider
{
    private static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromHours(96);
    private readonly IReadOnlyDictionary<string, ProviderLifecyclePolicy> policies = BuildPolicies(
        computrabajoOptions,
        adzunaOptions,
        greenhouseOptions,
        leverOptions);

    public ProviderLifecyclePolicy GetPolicy(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return new ProviderLifecyclePolicy("unknown", DefaultStaleAfter);
        }

        return policies.TryGetValue(sourceName.Trim(), out var policy)
            ? policy
            : new ProviderLifecyclePolicy(sourceName.Trim(), DefaultStaleAfter);
    }

    private static IReadOnlyDictionary<string, ProviderLifecyclePolicy> BuildPolicies(
        ComputrabajoProviderOptions computrabajoOptions,
        AdzunaProviderOptions adzunaOptions,
        GreenhouseProviderOptions greenhouseOptions,
        LeverProviderOptions leverOptions)
    {
        var map = new Dictionary<string, ProviderLifecyclePolicy>(StringComparer.OrdinalIgnoreCase)
        {
            [computrabajoOptions.SourceName] = CreatePolicy(computrabajoOptions.SourceName, computrabajoOptions.StaleAfterHours),
            [adzunaOptions.SourceName] = CreatePolicy(adzunaOptions.SourceName, adzunaOptions.StaleAfterHours),
            [greenhouseOptions.SourceName] = CreatePolicy(greenhouseOptions.SourceName, greenhouseOptions.StaleAfterHours),
            [leverOptions.SourceName] = CreatePolicy(leverOptions.SourceName, leverOptions.StaleAfterHours)
        };

        return map;
    }

    private static ProviderLifecyclePolicy CreatePolicy(string sourceName, int staleAfterHours)
    {
        var staleAfter = staleAfterHours <= 0
            ? DefaultStaleAfter
            : TimeSpan.FromHours(staleAfterHours);

        return new ProviderLifecyclePolicy(sourceName.Trim(), staleAfter);
    }
}
