using Microsoft.Extensions.DependencyInjection;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Collections;
using OpenJobEngine.Application.Jobs;

namespace OpenJobEngine.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenJobEngineApplication(this IServiceCollection services)
    {
        services.AddScoped<IJobCollectionService, JobCollectionService>();
        services.AddScoped<IJobQueryService, JobQueryService>();

        return services;
    }
}
