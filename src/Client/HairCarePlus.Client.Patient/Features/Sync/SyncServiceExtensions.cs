using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HairCarePlus.Client.Patient.Features.Sync;

public static class SyncServiceExtensions
{
    public static IServiceCollection AddSyncFeature(this IServiceCollection services)
    {
        services.AddSingleton<HairCarePlus.Shared.Communication.IOutboxRepository, OutboxRepository>();
        services.AddSingleton<ISyncApiClient, SyncApiClient>();
        services.AddSingleton<ILastSyncVersionStore, PreferencesSyncVersionStore>();
        services.AddSingleton<ISyncChangeApplier, SyncChangeApplier>();
        services.AddHostedService<SyncScheduler>();
        return services;
    }
} 