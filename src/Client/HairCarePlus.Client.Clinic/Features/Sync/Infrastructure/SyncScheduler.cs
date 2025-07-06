using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Sync.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

public class SyncScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SyncScheduler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                await syncService.SynchronizeAsync(stoppingToken);
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
} 