using System;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;
using HairCarePlus.Client.Clinic.Features.Sync.Application;
using Microsoft.Extensions.DependencyInjection;
using HairCarePlus.Shared.Common;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Features.Sync;

public static class SyncServiceExtensions
{
    public static IServiceCollection AddSyncFeature(this IServiceCollection services)
    {
        services.AddSingleton<HairCarePlus.Shared.Communication.IOutboxRepository, OutboxRepository>();
        services.AddHttpClient<ISyncHttpClient, SyncHttpClient>(client =>
        {
            var baseUrl = EnvironmentHelper.GetBaseApiUrl();
            client.BaseAddress = new Uri($"{baseUrl}/");
        });

        services.AddSingleton<ILastSyncVersionStore, PreferencesSyncVersionStore>();
        services.AddSingleton<ISyncChangeApplier, SyncChangeApplier>();

        services.AddScoped<ISyncService, SyncService>();
        services.AddHostedService<SyncScheduler>();
        return services;
    }
} 