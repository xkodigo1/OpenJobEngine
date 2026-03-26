namespace OpenJobEngine.Application.Common;

public sealed record WebhookTestResultDto(
    bool Success,
    int StatusCode,
    string Message);
