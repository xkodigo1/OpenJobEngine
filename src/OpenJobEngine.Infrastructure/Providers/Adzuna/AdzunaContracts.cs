using System.Text.Json.Serialization;

namespace OpenJobEngine.Infrastructure.Providers.Adzuna;

public sealed class AdzunaSearchResponse
{
    [JsonPropertyName("results")]
    public List<AdzunaJobContract> Results { get; init; } = [];
}

public sealed class AdzunaJobContract
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public string? Created { get; init; }

    [JsonPropertyName("salary_min")]
    public decimal? SalaryMin { get; init; }

    [JsonPropertyName("salary_max")]
    public decimal? SalaryMax { get; init; }

    [JsonPropertyName("contract_time")]
    public string? ContractTime { get; init; }

    [JsonPropertyName("contract_type")]
    public string? ContractType { get; init; }

    [JsonPropertyName("company")]
    public AdzunaNamedValue? Company { get; init; }

    [JsonPropertyName("category")]
    public AdzunaNamedValue? Category { get; init; }

    [JsonPropertyName("location")]
    public AdzunaLocationContract? Location { get; init; }
}

public sealed class AdzunaNamedValue
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }
}

public sealed class AdzunaLocationContract
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }
}
