using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenJobEngine.Api.Infrastructure;

internal static class HealthCheckResponseFactory
{
    public static object CreatePayload(HealthReport report)
    {
        return new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data.ToDictionary(pair => pair.Key, pair => pair.Value)
            })
        };
    }

    public static int GetStatusCode(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK,
            _ => StatusCodes.Status503ServiceUnavailable
        };
    }
}
