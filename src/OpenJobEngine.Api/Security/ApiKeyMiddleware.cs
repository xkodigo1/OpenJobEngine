using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using OpenJobEngine.Api.Infrastructure;
using OpenJobEngine.Api.Options;

namespace OpenJobEngine.Api.Security;

internal sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<ApiSecurityOptions> options,
    IProblemDetailsService problemDetailsService,
    ILogger<ApiKeyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var securityOptions = options.Value;

        if (!securityOptions.Enabled || IsBypassedPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(securityOptions.ApiKey))
        {
            logger.LogError("API key security is enabled but no API key value was configured.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = ApiProblemDetails.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Server misconfiguration",
                    "API key security is enabled but no API key value was configured.")
            });
            return;
        }

        if (!context.Request.Headers.TryGetValue(securityOptions.HeaderName, out var providedKey) ||
            !string.Equals(providedKey.ToString(), securityOptions.ApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = ApiProblemDetails.Create(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "A valid API key is required to access this resource.")
            });
            return;
        }

        await next(context);
    }

    private static bool IsBypassedPath(PathString path)
    {
        return path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }
}
