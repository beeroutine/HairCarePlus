using System;

namespace HairCarePlus.Client.Patient.Features.Chat.Models;

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

public class ChatMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime Timestamp { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string? RecipientId { get; set; }
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
    public bool IsRead { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? FileSize { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public ChatMessage? ReplyTo { get; set; }
}

public class Doctor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Specialty { get; set; }
    public required string PhotoUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }

    public Doctor()
    {
        Id = string.Empty;
        Name = string.Empty;
        Specialty = string.Empty;
        PhotoUrl = string.Empty;
    }
}

public enum AppointmentStatus
{
    Requested,
    Confirmed,
    Cancelled,
    Completed
}

public class Appointment
{
    public required string Id { get; set; }
    public DateTime DateTime { get; set; }
    public required string Purpose { get; set; }
    public AppointmentStatus Status { get; set; }
    public required string Notes { get; set; }

    public Appointment()
    {
        Id = string.Empty;
        Purpose = string.Empty;
        Notes = string.Empty;
        DateTime = DateTime.Now;
        Status = AppointmentStatus.Requested;
    }
} 