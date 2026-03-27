using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Providers;
using OpenJobEngine.Domain.Entities;

namespace OpenJobEngine.Infrastructure.DependencyInjection;

public sealed class OpenJobEngineDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<OpenJobEngineDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var jobSourceRepository = scope.ServiceProvider.GetRequiredService<IJobSourceRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var providers = scope.ServiceProvider.GetServices<IJobProvider>();

        foreach (var provider in providers)
        {
            var source = await jobSourceRepository.GetByNameAsync(provider.SourceName, cancellationToken);

            if (source is null)
            {
                await jobSourceRepository.AddAsync(
                    new JobSource(Guid.NewGuid(), provider.SourceName, "provider", true, $"Registered provider {provider.SourceName}"),
                    cancellationToken);
            }
            else
            {
                source.UpdateStatus(true, source.Description);
                await jobSourceRepository.UpdateAsync(source, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("OpenJobEngine provider catalog initialized");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
