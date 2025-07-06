using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;
using HairCarePlus.Shared.Communication.Sync;
using HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

namespace HairCarePlus.Client.Clinic.Features.Sync.Application;

public interface ISyncService
{
    Task SynchronizeAsync(CancellationToken cancellationToken);
}

public class SyncService : ISyncService
{
    private readonly IOutboxRepository _outbox;
    private readonly ISyncHttpClient _syncClient;
    // ... IDataStore or other services to apply changes

    public SyncService(IOutboxRepository outbox, ISyncHttpClient syncClient)
    {
        _outbox = outbox;
        _syncClient = syncClient;
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        var pendingItems = await _outbox.GetPendingItemsAsync();
        if (pendingItems.Count == 0) return;

        var request = new BatchSyncRequestDto
        {
            // Group items by type and deserialize
        };

        var response = await _syncClient.PushAsync(request, cancellationToken);

        if (response != null)
        {
            // Apply server changes
            // Mark items as Acked
            await _outbox.UpdateStatusAsync(pendingItems.Select(i => i.Id), SyncStatus.Acked);
        }
    }
} 