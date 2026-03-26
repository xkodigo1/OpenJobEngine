using OpenJobEngine.Application;
using OpenJobEngine.Infrastructure.DependencyInjection;
using OpenJobEngine.Worker.Options;
using OpenJobEngine.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CollectionWorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.AddOpenJobEngineApplication();
builder.Services.AddOpenJobEngineInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ScheduledCollectionWorker>();

var host = builder.Build();
await host.RunAsync();
