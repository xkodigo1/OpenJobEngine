namespace OpenJobEngine.Api.Contracts;

public sealed record WebhookTestApiRequest(string Url, string? Secret);
