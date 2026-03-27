namespace OpenJobEngine.Application.Abstractions.Providers;

public interface IProviderLifecyclePolicyProvider
{
    ProviderLifecyclePolicy GetPolicy(string sourceName);
}

public sealed record ProviderLifecyclePolicy(
    string SourceName,
    TimeSpan StaleAfter);
