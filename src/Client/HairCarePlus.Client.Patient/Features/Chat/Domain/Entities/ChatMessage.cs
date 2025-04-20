using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

public enum MessageType
{
    Text,
    Image,
    Video,
    File,
    System
}

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed
}

public enum SyncStatus
{
    NotSynced,
    Syncing,
    Synced,
    Failed
}

[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LocalId { get; set; }
    
    public string? ServerMessageId { get; set; }
    
    [Required]
    public string ConversationId { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public DateTime SentAt { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string SenderId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? RecipientId { get; set; }
    
    public MessageType Type { get; set; }
    
    public MessageStatus Status { get; set; }
    
    public SyncStatus SyncStatus { get; set; }
    
    public bool IsRead { get; set; }
    
    public string? AttachmentUrl { get; set; }
    
    public string? LocalAttachmentPath { get; set; }
    
    public string? ThumbnailUrl { get; set; }
    
    public string? LocalThumbnailPath { get; set; }
    
    public long? FileSize { get; set; }
    
    [MaxLength(255)]
    public string? FileName { get; set; }
    
    [MaxLength(100)]
    public string? MimeType { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public int? ReplyToLocalId { get; set; }
    
    [ForeignKey("ReplyToLocalId")]
    public ChatMessage? ReplyTo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastModifiedAt { get; set; }
    
    [NotMapped]
    public bool IsSyncPending => SyncStatus == SyncStatus.NotSynced || SyncStatus == SyncStatus.Failed;
    
    [NotMapped]
    public bool IsLocalOnly => ServerMessageId == null;
} 