using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

public interface ILastSyncVersionStore
{
    Task<long> GetAsync();
    Task SetAsync(long version);
}

/// <summary>
/// Stores last successful sync version (Unix milliseconds) in Preferences API on device.
/// </summary>
public sealed class PreferencesSyncVersionStore : ILastSyncVersionStore
{
    private const string Key = "LastSyncVersion";

    public Task<long> GetAsync()
    {
        var value = Preferences.Get(Key, 0L);
        return Task.FromResult(value);
    }

    public Task SetAsync(long version)
    {
        Preferences.Set(Key, version);
        return Task.CompletedTask;
    }
} 