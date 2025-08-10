using System;

namespace HairCarePlus.Shared.Communication;

public class ChatMessageDto
{
    public int LocalId { get; set; }
    public string? ServerMessageId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime Timestamp { get; set; }
    public string SenderId { get; set; } = string.Empty;
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
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int? ReplyToLocalId { get; set; }
    public ChatMessageDto? ReplyTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public bool IsSyncPending => SyncStatus == SyncStatus.NotSynced || SyncStatus == SyncStatus.Failed;
    public bool IsLocalOnly => ServerMessageId == null;
} 