namespace OpenJobEngine.Api.Options;

public sealed class ApiSecurityOptions
{
    public bool Enabled { get; set; }

    public string HeaderName { get; set; } = "X-Api-Key";

    public string ApiKey { get; set; } = string.Empty;
}
