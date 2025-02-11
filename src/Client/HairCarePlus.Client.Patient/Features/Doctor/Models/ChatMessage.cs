using System;

namespace HairCarePlus.Client.Patient.Features.Doctor.Models
{
    public class ChatMessage
    {
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageType Type { get; set; }
        public string AttachmentUrl { get; set; }
        public bool IsRead { get; set; }
        public bool IsSending { get; set; }
    }

    public class Doctor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public enum MessageType
    {
        Text,
        Photo,
        Document,
        Appointment
    }

    public class Appointment
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Purpose { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; }
    }

    public enum AppointmentStatus
    {
        Requested,
        Confirmed,
        Cancelled,
        Completed
    }
} 