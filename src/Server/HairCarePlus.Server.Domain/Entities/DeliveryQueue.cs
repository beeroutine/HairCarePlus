using System;

namespace HairCarePlus.Server.Domain.Entities;

/// <summary>
///   Transient envelope that хранится на сервере только до тех пор,
///   пока все назначенные получатели не подтвердят получение (ACK)
///   либо не выйдет TTL.
/// </summary>
public class DeliveryQueue : BaseEntity
{
    /// <summary>
    ///   Тип передаваемой сущности (PhotoReport, TaskReport и т.д.)
    /// </summary>
    public string EntityType { get; set; } = null!;

    /// <summary>
    ///   JSON-представление сущности или метаданных.
    ///   Для больших медиа вместо этого используется <see cref="BlobUrl"/>.
    /// </summary>
    public string PayloadJson { get; set; } = null!;

    /// <summary>
    ///   Если файл загружен отдельно – ссылка на него.
    /// </summary>
    public string? BlobUrl { get; set; }

    public Guid PatientId { get; set; }

    /// <summary>
    /// Битовая маска ожидаемых получателей (1 – Clinic, 2 – Patient и т.д.)
    /// </summary>
    public byte ReceiversMask { get; set; }

    /// <summary>
    /// Битовая маска уже подтвердивших получение.
    /// </summary>
    public byte DeliveredMask { get; set; }

    /// <summary>
    /// Время, после которого запись можно безболезненно удалить,
    /// даже если ACK не пришёл.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
} 