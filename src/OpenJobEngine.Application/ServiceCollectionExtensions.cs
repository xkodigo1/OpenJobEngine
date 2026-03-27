using Microsoft.Extensions.DependencyInjection;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Collections;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Jobs;
using OpenJobEngine.Application.Profiles;
using OpenJobEngine.Application.Resume;

namespace OpenJobEngine.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenJobEngineApplication(this IServiceCollection services)
    {
        services.AddScoped<IJobCollectionService, JobCollectionService>();
        services.AddScoped<IAlertDispatchService, AlertDispatchService>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.AddScoped<ICandidateProfileService, CandidateProfileService>();
        services.AddScoped<IResumeImportService, ResumeImportService>();
        services.AddScoped<ISystemMetricsService, SystemMetricsService>();

        return services;
    }
}
