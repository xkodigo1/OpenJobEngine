using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using OpenJobEngine.Api.Infrastructure;
using OpenJobEngine.Application;
using OpenJobEngine.Infrastructure.DependencyInjection;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var apiAssembly = Assembly.GetExecutingAssembly();
var apiVersion = (apiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? apiAssembly.GetName().Version?.ToString()
    ?? "0.0.0")
    .Split('+', 2)[0];

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
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-OpenJobEngine-Version"] = apiVersion;
    await next();
});

app.UseExceptionHandler();
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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "OpenJobEngine API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"OpenJobEngine API {apiVersion}");
});

app.MapControllers();

app.Run();

public partial class Program;
