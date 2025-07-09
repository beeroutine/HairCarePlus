using System;

namespace HairCarePlus.Shared.Communication.Sync;

/// <summary>
///  Обёртка, доставляемая через BatchSync. Клиент должен десериализовать PayloadJson
///  согласно EntityType, применить локально и потом отправить ACK(Id).
/// </summary>
public sealed class DeliveryPacketDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public string? BlobUrl { get; init; }
} 