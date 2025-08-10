using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;

[Table("OutboxItems")]
public class OutboxItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public HairCarePlus.Shared.Communication.OutboxStatus Status { get; set; } = HairCarePlus.Shared.Communication.OutboxStatus.Pending;

    public int RetryCount { get; set; }

    // --- Aliases for legacy code (will be removed once refactor completes) ---
    public string PayloadJson
    {
        get => Payload;
        set => Payload = value;
    }

    public string LocalEntityId { get; set; } = string.Empty;

    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(Payload);
} 