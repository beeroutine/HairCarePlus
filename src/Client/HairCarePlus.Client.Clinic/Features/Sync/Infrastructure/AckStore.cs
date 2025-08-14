using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Clinic.Features.Sync.Infrastructure;

public interface IAckStore
{
	Task<IReadOnlyList<Guid>> LoadAsync();
	Task AddAsync(Guid id);
	Task RemoveAsync(IEnumerable<Guid> ids);
	Task ClearAsync();
}

/// <summary>
/// Preferences-based persistent storage for pending DeliveryQueue ACK ids.
/// Ensures ACKs survive app restarts, so server can purge ephemeral files.
/// </summary>
public sealed class PreferencesAckStore : IAckStore
{
	private const string Key = "PendingAckIds";

	public Task<IReadOnlyList<Guid>> LoadAsync()
	{
		try
		{
			var json = Preferences.Get(Key, string.Empty);
			if (string.IsNullOrWhiteSpace(json))
				return Task.FromResult((IReadOnlyList<Guid>)Array.Empty<Guid>());
			var list = JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
			return Task.FromResult((IReadOnlyList<Guid>)list);
		}
		catch
		{
			return Task.FromResult((IReadOnlyList<Guid>)Array.Empty<Guid>());
		}
	}

	public Task AddAsync(Guid id)
	{
		try
		{
			var current = ReadList();
			if (!current.Contains(id))
			{
				current.Add(id);
				WriteList(current);
			}
		}
		catch { }
		return Task.CompletedTask;
	}

	public Task RemoveAsync(IEnumerable<Guid> ids)
	{
		try
		{
			var set = new HashSet<Guid>(ids);
			var current = ReadList();
			if (current.RemoveAll(g => set.Contains(g)) > 0)
			{
				WriteList(current);
			}
		}
		catch { }
		return Task.CompletedTask;
	}

	public Task ClearAsync()
	{
		try { Preferences.Remove(Key); } catch { }
		return Task.CompletedTask;
	}

	private static List<Guid> ReadList()
	{
		try
		{
			var json = Preferences.Get(Key, string.Empty);
			if (string.IsNullOrWhiteSpace(json)) return new List<Guid>();
			return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
		}
		catch { return new List<Guid>(); }
	}

	private static void WriteList(List<Guid> list)
	{
		try
		{
			var json = JsonSerializer.Serialize(list);
			Preferences.Set(Key, json);
		}
		catch { }
	}
}


