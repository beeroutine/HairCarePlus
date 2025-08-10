using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Features.Chat.Models;

public sealed class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LocalId { get; set; }

    [MaxLength(50)]
    public string ConversationId { get; set; } = "default_conversation";

    public required string SenderId { get; init; }
    public required string Content { get; init; }

    // UTC time when message was sent (client clock)
    public DateTimeOffset SentAt { get; init; }
    public DateTime Timestamp => SentAt.LocalDateTime;

    // Creation/last-update timestamps for local DB bookkeeping (identично пациентскому приложению)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }

    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;

    // Delivery status (Sent / Delivered / Read)
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    // Whether recipient read the message
    public bool IsRead { get; set; } = false;

    // Message kind (text / image / file…)
    public MessageType Type { get; init; } = MessageType.Text;

    // FK to replied message (nullable)
    public int? ReplyToLocalId { get; set; }
    [ForeignKey(nameof(ReplyToLocalId))]
    public ChatMessage? ReplyTo { get; init; }

    // Local path to an attachment (optional)
    public string? LocalAttachmentPath { get; init; }

    public bool IsOutgoing(string currentUserId) => SenderId == currentUserId;
}