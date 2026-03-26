using OpenJobEngine.Application;
using OpenJobEngine.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenJobEngineApplication();
builder.Services.AddOpenJobEngineInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OpenJobEngine API",
        Version = "v1",
        Description = "Backend-first API for multi-source job aggregation, enrichment, candidate profiles, resume parsing, and explainable matching for tech talent."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "OpenJobEngine API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenJobEngine API v1");
});

app.MapControllers();

app.Run();
