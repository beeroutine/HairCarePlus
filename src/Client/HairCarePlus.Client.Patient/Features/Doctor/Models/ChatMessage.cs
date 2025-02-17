using System;

namespace HairCarePlus.Client.Patient.Features.Doctor.Models
{
    public class ChatMessage
    {
        public required string Id { get; set; }
        public required string SenderId { get; set; }
        public required string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public MessageType Type { get; set; }
        public string? AttachmentUrl { get; set; }
        public ChatMessage ReplyTo { get; set; }
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

    public enum MessageType
    {
        Text,
        Image,
        Appointment
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

    public enum AppointmentStatus
    {
        Requested,
        Confirmed,
        Cancelled,
        Completed
    }
} 