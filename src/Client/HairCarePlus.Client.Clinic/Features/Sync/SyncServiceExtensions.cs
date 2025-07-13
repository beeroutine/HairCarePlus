using System;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;
using HairCarePlus.Client.Clinic.Features.Sync.Application;
using Microsoft.Extensions.DependencyInjection;

namespace HairCarePlus.Client.Clinic.Features.Sync;

public static class SyncServiceExtensions
{
    public static IServiceCollection AddSyncFeature(this IServiceCollection services)
    {
        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddHttpClient<ISyncHttpClient, SyncHttpClient>(client =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://10.153.34.67:5281/";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddSingleton<ILastSyncVersionStore, PreferencesSyncVersionStore>();
        services.AddSingleton<ISyncChangeApplier, SyncChangeApplier>();

        services.AddScoped<ISyncService, SyncService>();
        services.AddHostedService<SyncScheduler>();
        return services;
    }
} 