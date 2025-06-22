using System;
namespace HairCarePlus.Shared.Communication;

public sealed class ChatMessageDto
{
    public required string ConversationId { get; init; }
    public required string SenderId { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset SentAt { get; init; }

    // Optional reply metadata
    public string? ReplyToSenderId { get; init; }
    public string? ReplyToContent { get; init; }
} 