using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenJobEngine.Api.Health;
using OpenJobEngine.Api.Infrastructure;
using OpenJobEngine.Api.Options;
using OpenJobEngine.Api.Security;
using OpenJobEngine.Application;
using OpenJobEngine.Infrastructure.Catalog;
using OpenJobEngine.Infrastructure.DependencyInjection;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var apiAssembly = Assembly.GetExecutingAssembly();
var apiVersion = (apiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? apiAssembly.GetName().Version?.ToString()
    ?? "0.0.0")
    .Split('+', 2)[0];
var apiSecurityOptions = builder.Configuration.GetSection("ApiSecurity").Get<ApiSecurityOptions>() ?? new ApiSecurityOptions();

apiSecurityOptions.HeaderName = string.IsNullOrWhiteSpace(apiSecurityOptions.HeaderName)
    ? "X-Api-Key"
    : apiSecurityOptions.HeaderName.Trim();

if (apiSecurityOptions.Enabled && string.IsNullOrWhiteSpace(apiSecurityOptions.ApiKey))
{
    throw new InvalidOperationException("ApiSecurity:ApiKey must be configured when ApiSecurity:Enabled is true.");
}

builder.Services.Configure<ApiSecurityOptions>(builder.Configuration.GetSection("ApiSecurity"));
builder.Services.AddOpenJobEngineApplication();
builder.Services.AddOpenJobEngineInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<OpenJobEngineExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["version"] = apiVersion;
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    };
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation failed",
            detail: "One or more request fields are invalid.");

        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["version"] = apiVersion;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseConnectivityHealthCheck>("database")
    .AddCheck<CatalogHealthCheck>("catalogs")
    .AddCheck<MatchingRulesHealthCheck>("matching-rules");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = ApiProblemDetails.CreateForStatusCode(context.HttpContext, StatusCodes.Status429TooManyRequests)
        });
    };

    options.AddPolicy("collections", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("resume-import", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("webhook-test", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    foreach (var xmlPath in Directory.GetFiles(AppContext.BaseDirectory, "OpenJobEngine*.xml"))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OpenJobEngine API",
        Version = apiVersion,
        Description = "Backend-first API for multi-source job aggregation, enrichment, candidate profiles, resume parsing, and explainable matching for tech talent."
    });

    if (apiSecurityOptions.Enabled)
    {
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Name = apiSecurityOptions.HeaderName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Provide the configured API key in the request header."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            }] = Array.Empty<string>()
        });
    }
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-OpenJobEngine-Version"] = apiVersion;
    await next();
});

app.UseExceptionHandler();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseRateLimiter();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;

    if (response.StatusCode < 400 || response.HasStarted || response.ContentLength.HasValue)
    {
        return;
    }

    var problemDetailsService = statusCodeContext.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = statusCodeContext.HttpContext,
        ProblemDetails = ApiProblemDetails.CreateForStatusCode(statusCodeContext.HttpContext, response.StatusCode)
    });
});

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .WithName("HealthLive");

app.MapGet("/health/ready", async (HealthCheckService healthCheckService, HttpContext context) =>
{
    var report = await healthCheckService.CheckHealthAsync(_ => true, context.RequestAborted);
    var payload = HealthCheckResponseFactory.CreatePayload(report);
    context.Response.Headers["Cache-Control"] = "no-store";
    return Results.Json(payload, statusCode: HealthCheckResponseFactory.GetStatusCode(report.Status));
})
    .WithName("HealthReady");

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "OpenJobEngine API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"OpenJobEngine API {apiVersion}");
});

app.MapControllers();

app.Run();

public partial class Program;
