using Microsoft.AspNetCore.Mvc;

namespace OpenJobEngine.Api.Infrastructure;

internal static class ApiProblemDetails
{
    public static ProblemDetails Create(HttpContext httpContext, int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };
    }

    public static ProblemDetails CreateForStatusCode(HttpContext httpContext, int statusCode)
    {
        var (title, detail) = statusCode switch
        {
            StatusCodes.Status400BadRequest => ("Bad request", "The request could not be processed."),
            StatusCodes.Status401Unauthorized => ("Unauthorized", "Authentication is required to access this resource."),
            StatusCodes.Status403Forbidden => ("Forbidden", "The current principal is not allowed to access this resource."),
            StatusCodes.Status404NotFound => ("Resource not found", "The requested resource was not found."),
            StatusCodes.Status405MethodNotAllowed => ("Method not allowed", "The requested HTTP method is not supported for this resource."),
            StatusCodes.Status415UnsupportedMediaType => ("Unsupported media type", "The request payload format is not supported."),
            StatusCodes.Status429TooManyRequests => ("Too many requests", "The request rate limit was exceeded."),
            _ when statusCode >= 500 => ("Server error", "The server failed to process the request."),
            _ => ("Request failed", "The request did not complete successfully.")
        };

        return Create(httpContext, statusCode, title, detail);
    }
}
