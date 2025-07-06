using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Patient.Features.Sync.Infrastructure;

public interface ILastSyncVersionStore
{
    Task<long> GetAsync();
    Task SetAsync(long version);
}

/// <summary>
/// Persists last successful sync timestamp (Unix milliseconds) using Preferences API.
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