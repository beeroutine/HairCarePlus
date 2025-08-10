using System;

namespace HairCarePlus.Shared.Communication;

/// <summary>
/// DTO representing a unit of work waiting in Outbox to be synced with the server.
/// This same format is stored locally and transmitted over network.
/// </summary>
public class OutboxItemDto
{
    // Alias for legacy code until full refactor
    public string PayloadJson { get => Payload; set => Payload = value; }
    public int Id { get; set; }

    /// <summary>Logical type of payload, e.g. "ChatMessage"</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>JSON payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>UTC creation time on the device.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC last modification time.</summary>
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Local identifier of related entity (string to support Guid or int).</summary>
    public string LocalEntityId { get; set; } = string.Empty;

    /// <summary>Status in outbox processing pipeline.</summary>
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    /// <summary>Number of send retries.</summary>
    public int RetryCount { get; set; }
}
