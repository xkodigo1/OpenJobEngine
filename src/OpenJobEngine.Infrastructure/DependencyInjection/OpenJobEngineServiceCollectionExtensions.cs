using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Infrastructure.Catalog;
using OpenJobEngine.Infrastructure.Options;
using OpenJobEngine.Infrastructure.Matching;
using OpenJobEngine.Infrastructure.Persistence;
using OpenJobEngine.Infrastructure.Persistence.Repositories;
using OpenJobEngine.Infrastructure.Providers;
using OpenJobEngine.Infrastructure.Providers.Adzuna;
using OpenJobEngine.Infrastructure.Providers.Computrabajo;
using OpenJobEngine.Infrastructure.Providers.Greenhouse;
using OpenJobEngine.Infrastructure.Resume;
using OpenJobEngine.Infrastructure.Services;

namespace OpenJobEngine.Infrastructure.DependencyInjection;

public static class OpenJobEngineServiceCollectionExtensions
{
    public static IServiceCollection AddOpenJobEngineInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var persistenceOptions = configuration.GetSection("Persistence").Get<PersistenceOptions>() ?? new PersistenceOptions();
        services.Configure<PersistenceOptions>(configuration.GetSection("Persistence"));

        services.Configure<ProviderCatalogOptions>(configuration.GetSection("Providers"));
        services.Configure<ComputrabajoProviderOptions>(configuration.GetSection("Providers:Computrabajo"));
        services.Configure<AdzunaProviderOptions>(configuration.GetSection("Providers:Adzuna"));
        services.Configure<GreenhouseProviderOptions>(configuration.GetSection("Providers:Greenhouse"));

        services.AddDbContext<OpenJobEngineDbContext>(options =>
        {
            var provider = persistenceOptions.Provider?.Trim() ?? "Sqlite";

            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                var postgresConnectionString = configuration.GetConnectionString("Postgres")
                    ?? configuration["ConnectionStrings:Postgres"]
                    ?? configuration["ConnectionStrings__Postgres"]
                    ?? throw new InvalidOperationException("Postgres connection string was not configured.");

                options.UseNpgsql(postgresConnectionString);
                return;
            }

            var sqliteConnectionString = configuration.GetConnectionString("Sqlite")
                ?? configuration["ConnectionStrings:Sqlite"]
                ?? configuration["ConnectionStrings__Sqlite"]
                ?? "Data Source=openjobengine.db";

            options.UseSqlite(sqliteConnectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OpenJobEngineDbContext>());
        services.AddScoped<IJobRepository, EfJobRepository>();
        services.AddScoped<IScrapeExecutionRepository, EfScrapeExecutionRepository>();
        services.AddScoped<IJobSourceRepository, EfJobSourceRepository>();
        services.AddScoped<ICandidateProfileRepository, EfCandidateProfileRepository>();
        services.AddScoped<IMatchExecutionRepository, EfMatchExecutionRepository>();
        services.AddScoped<INormalizationService, DefaultNormalizationService>();
        services.AddScoped<IJobEnrichmentService, DefaultJobEnrichmentService>();
        services.AddScoped<IDeduplicationService, DefaultDeduplicationService>();
        services.AddScoped<IMatchingService, DeterministicMatchingService>();
        services.AddScoped<IResumeTextExtractor, PdfPigResumeTextExtractor>();
        services.AddScoped<IResumeProfileExtractor, HeuristicResumeProfileExtractor>();
        services.AddScoped<IWebhookTestService, WebhookTestService>();
        services.AddHttpClient();
        services.AddSingleton<ITechnologyTaxonomyProvider, JsonTechnologyTaxonomyProvider>();
        services.AddSingleton<IMatchingRulesProvider, JsonMatchingRulesProvider>();

        services.AddSingleton<ComputrabajoHtmlParser>();
        services.AddSingleton<IPageContentFetcher, PlaywrightPageContentFetcher>();
        services.AddHostedService<OpenJobEngineDatabaseMigrator>();
        services.AddHostedService<OpenJobEngineDatabaseInitializer>();

        RegisterProviders(services, configuration);

        return services;
    }

    private static void RegisterProviders(IServiceCollection services, IConfiguration configuration)
    {
        var computrabajoOptions = configuration.GetSection("Providers:Computrabajo").Get<ComputrabajoProviderOptions>() ?? new();
        var adzunaOptions = configuration.GetSection("Providers:Adzuna").Get<AdzunaProviderOptions>() ?? new();
        var greenhouseOptions = configuration.GetSection("Providers:Greenhouse").Get<GreenhouseProviderOptions>() ?? new();

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

        if (greenhouseOptions.Enabled)
        {
            services.AddSingleton(greenhouseOptions);
            services.AddHttpClient<GreenhouseJobProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<IJobProvider>(sp => sp.GetRequiredService<GreenhouseJobProvider>());
        }
    }
}
