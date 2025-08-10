using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

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

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int RetryCount { get; set; }

    // Legacy compatibility
    public string PayloadJson
    {
        get => Payload;
        set => Payload = value;
    }

    public string LocalEntityId { get; set; } = string.Empty;

    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(Payload);
}