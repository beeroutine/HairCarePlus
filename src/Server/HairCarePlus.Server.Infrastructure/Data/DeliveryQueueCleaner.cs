using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Server.Infrastructure.Data;

public class DeliveryQueueCleaner : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DeliveryQueueCleaner> _logger;

    public DeliveryQueueCleaner(IServiceProvider sp, ILogger<DeliveryQueueCleaner> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDeliveryQueueRepository>();
                await repo.RemoveExpiredAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeliveryQueue cleanup failed");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
} 