using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Api.Infrastructure;

internal sealed class OpenJobEngineExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<OpenJobEngineExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, logLevel) = exception switch
        {
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message, LogLevel.Information),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation", exception.Message, LogLevel.Warning),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred while processing the request.", LogLevel.Error)
        };

        logger.Log(logLevel, exception, "Request failed with status code {StatusCode}", statusCode);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = ApiProblemDetails.Create(httpContext, statusCode, title, detail);

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}
