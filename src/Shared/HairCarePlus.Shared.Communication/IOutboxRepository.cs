using System.Collections.Generic;
using System.Threading.Tasks;

namespace HairCarePlus.Shared.Communication;

/// <summary>
/// Contract for client‐side Outbox storage.
/// Each platform provides its own EF/SQLite implementation.
/// </summary>
public interface IOutboxRepository
{
    Task AddAsync(OutboxItemDto item);
    Task<IReadOnlyList<OutboxItemDto>> GetPendingItemsAsync();
    Task UpdateStatusAsync(IEnumerable<int> ids, OutboxStatus status);
    Task DeleteAsync(IEnumerable<int> ids);
}
