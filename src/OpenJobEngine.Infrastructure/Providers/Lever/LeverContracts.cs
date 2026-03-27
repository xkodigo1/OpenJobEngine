using System.Text.Json.Serialization;

namespace OpenJobEngine.Infrastructure.Providers.Lever;

public sealed class LeverJobPostingsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("categories")]
    public LeverCategoriesContract Categories { get; init; } = new();

    [JsonPropertyName("openingPlain")]
    public string? OpeningPlain { get; init; }

    [JsonPropertyName("descriptionPlain")]
    public string? DescriptionPlain { get; init; }

    [JsonPropertyName("descriptionBodyPlain")]
    public string? DescriptionBodyPlain { get; init; }

    [JsonPropertyName("additionalPlain")]
    public string? AdditionalPlain { get; init; }

    [JsonPropertyName("hostedUrl")]
    public string? HostedUrl { get; init; }

    [JsonPropertyName("applyUrl")]
    public string? ApplyUrl { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("workplaceType")]
    public string? WorkplaceType { get; init; }

    [JsonPropertyName("salaryRange")]
    public LeverSalaryRangeContract? SalaryRange { get; init; }

    [JsonPropertyName("salaryDescriptionPlain")]
    public string? SalaryDescriptionPlain { get; init; }

    [JsonPropertyName("lists")]
    public List<LeverJobListContract>? Lists { get; init; }
}

public sealed class LeverCategoriesContract
{
    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("commitment")]
    public string? Commitment { get; init; }

    [JsonPropertyName("team")]
    public string? Team { get; init; }

    [JsonPropertyName("department")]
    public string? Department { get; init; }

    [JsonPropertyName("allLocations")]
    public List<LeverNamedValueContract>? AllLocations { get; init; }
}

public sealed class LeverNamedValueContract
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

public sealed class LeverSalaryRangeContract
{
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("interval")]
    public string? Interval { get; init; }

    [JsonPropertyName("min")]
    public decimal? Min { get; init; }

    [JsonPropertyName("max")]
    public decimal? Max { get; init; }
}

public sealed class LeverJobListContract
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
