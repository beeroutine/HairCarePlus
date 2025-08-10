namespace HairCarePlus.Shared.Communication;

/// <summary>
/// Unified status for Outbox items across all clients.
/// </summary>
public enum OutboxStatus
{
    /// <summary>
    /// Item is queued locally and has not yet been sent to the server.
    /// </summary>
    Pending,

    /// <summary>
    /// Item has been sent but not yet acknowledged by the server.
    /// </summary>
    Sent,

    /// <summary>
    /// Server acknowledged successful processing of this item.
    /// </summary>
    Acked,

    /// <summary>
    /// Final failure after retries.
    /// </summary>
    Failed
}
