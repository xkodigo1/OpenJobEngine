using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Infrastructure.Options;
using OpenJobEngine.Infrastructure.Persistence;
using OpenJobEngine.Infrastructure.Persistence.Repositories;
using OpenJobEngine.Infrastructure.Providers;
using OpenJobEngine.Infrastructure.Providers.Adzuna;
using OpenJobEngine.Infrastructure.Providers.Computrabajo;
using OpenJobEngine.Infrastructure.Services;

namespace OpenJobEngine.Infrastructure.DependencyInjection;

public static class OpenJobEngineServiceCollectionExtensions
{
    public static IServiceCollection AddOpenJobEngineInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["ConnectionStrings:Postgres"]
            ?? configuration["ConnectionStrings__Postgres"]
            ?? throw new InvalidOperationException("Postgres connection string was not configured.");

        services.Configure<ProviderCatalogOptions>(configuration.GetSection("Providers"));
        services.Configure<ComputrabajoProviderOptions>(configuration.GetSection("Providers:Computrabajo"));
        services.Configure<AdzunaProviderOptions>(configuration.GetSection("Providers:Adzuna"));

        services.AddDbContext<OpenJobEngineDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OpenJobEngineDbContext>());
        services.AddScoped<IJobRepository, EfJobRepository>();
        services.AddScoped<IScrapeExecutionRepository, EfScrapeExecutionRepository>();
        services.AddScoped<IJobSourceRepository, EfJobSourceRepository>();
        services.AddScoped<INormalizationService, DefaultNormalizationService>();
        services.AddScoped<IDeduplicationService, DefaultDeduplicationService>();

        services.AddSingleton<ComputrabajoHtmlParser>();
        services.AddSingleton<IPageContentFetcher, PlaywrightPageContentFetcher>();
        services.AddHostedService<OpenJobEngineDatabaseInitializer>();

        RegisterProviders(services, configuration);

        return services;
    }

    private static void RegisterProviders(IServiceCollection services, IConfiguration configuration)
    {
        var computrabajoOptions = configuration.GetSection("Providers:Computrabajo").Get<ComputrabajoProviderOptions>() ?? new();
        var adzunaOptions = configuration.GetSection("Providers:Adzuna").Get<AdzunaProviderOptions>() ?? new();

        if (computrabajoOptions.Enabled)
        {
            services.AddSingleton(computrabajoOptions);
            services.AddHttpClient<ComputrabajoProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(computrabajoOptions.UserAgent);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<IJobProvider>(sp => sp.GetRequiredService<ComputrabajoProvider>());
        }

        if (adzunaOptions.Enabled)
        {
            services.AddSingleton(adzunaOptions);
            services.AddHttpClient<AdzunaJobProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<IJobProvider>(sp => sp.GetRequiredService<AdzunaJobProvider>());
        }
    }
}
