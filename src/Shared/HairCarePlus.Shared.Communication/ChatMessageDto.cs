using System;
namespace HairCarePlus.Shared.Communication;

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public MessageStatus Status { get; set; }

    // Optional reply metadata
    public string? ReplyToSenderId { get; init; }
    public string? ReplyToContent { get; init; }
}

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed
} 