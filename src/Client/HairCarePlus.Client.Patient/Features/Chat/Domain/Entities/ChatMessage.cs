using System;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentTime { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public MessageStatus Status { get; set; }
    public string? MediaUrl { get; set; }
    public DateTime? ReadTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

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