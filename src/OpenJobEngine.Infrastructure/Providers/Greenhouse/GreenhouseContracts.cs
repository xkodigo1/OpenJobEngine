using System.Text.Json.Serialization;

namespace OpenJobEngine.Infrastructure.Providers.Greenhouse;

public sealed class GreenhouseJobsResponse
{
    [JsonPropertyName("jobs")]
    public List<GreenhouseJobContract> Jobs { get; init; } = [];
}

public sealed class GreenhouseJobContract
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("internal_job_id")]
    public long? InternalJobId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; init; }

    [JsonPropertyName("requisition_id")]
    public string? RequisitionId { get; init; }

    [JsonPropertyName("location")]
    public GreenhouseNamedLocation? Location { get; init; }

    [JsonPropertyName("absolute_url")]
    public string AbsoluteUrl { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("departments")]
    public List<GreenhouseNamedEntity>? Departments { get; init; }

    [JsonPropertyName("offices")]
    public List<GreenhouseOfficeContract>? Offices { get; init; }
}

public sealed class GreenhouseNamedLocation
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed class GreenhouseNamedEntity
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed class GreenhouseOfficeContract
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }
}
