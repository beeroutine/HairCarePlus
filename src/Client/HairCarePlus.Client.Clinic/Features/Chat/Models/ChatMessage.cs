using System;

namespace HairCarePlus.Client.Clinic.Features.Chat.Models;

public sealed class ChatMessage
{
    public required string SenderId { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset SentAt { get; init; }
    public DateTime Timestamp => SentAt.LocalDateTime;

    // Indicates the type of the message (text, image, etc.)
    public MessageType Type { get; init; } = MessageType.Text;

    // Reference to the message this one replies to (if any)
    public ChatMessage? ReplyTo { get; init; }

    // Local path to an attachment (image/video) stored on the device
    public string? LocalAttachmentPath { get; init; }

    public bool IsOutgoing(string currentUserId) => SenderId == currentUserId;
} 