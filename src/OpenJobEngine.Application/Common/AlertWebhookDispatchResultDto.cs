namespace OpenJobEngine.Application.Common;

public sealed record AlertWebhookDispatchResultDto(
    bool Success,
    int? StatusCode,
    string? ResponseBody,
    string? ErrorMessage);
