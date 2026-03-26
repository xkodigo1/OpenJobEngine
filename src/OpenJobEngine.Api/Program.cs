using OpenJobEngine.Application;
using OpenJobEngine.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var apiAssembly = Assembly.GetExecutingAssembly();
var apiVersion = apiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? apiAssembly.GetName().Version?.ToString()
    ?? "0.0.0";

builder.Services.AddOpenJobEngineApplication();
builder.Services.AddOpenJobEngineInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "OpenJobEngine API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"OpenJobEngine API {apiVersion}");
});

app.MapControllers();

app.Run();
