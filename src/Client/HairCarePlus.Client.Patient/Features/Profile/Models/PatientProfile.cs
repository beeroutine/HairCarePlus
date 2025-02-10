using System;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Profile.Models
{
    public class PatientProfile
    {
        public required string Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime SurgeryDate { get; set; }
        public required string PreferredLanguage { get; set; }
        public required string TimeZoneId { get; set; }
        public string? PhotoUrl { get; set; }
        public ProfileStatus Status { get; set; }
        public DateTime LastSyncTime { get; set; }
        public List<string> Medications { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    public enum ProfileStatus
    {
        Active,
        PendingVerification,
        Inactive,
        Blocked
    }
} 