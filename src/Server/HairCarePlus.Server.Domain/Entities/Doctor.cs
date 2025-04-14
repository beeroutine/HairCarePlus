using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.ValueObjects;

namespace HairCarePlus.Server.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Specialty { get; private set; }
        public string PhotoUrl { get; private set; }
        public bool IsOnline { get; private set; }
        public DateTime LastSeen { get; private set; }
        public List<Patient> Patients { get; private set; }
        public List<ChatMessage> ChatMessages { get; private set; }
        public List<TreatmentSchedule> TreatmentSchedules { get; private set; }
        public List<string> Certifications { get; private set; }
        public List<string> Languages { get; private set; }
        public TimeZoneInfo TimeZone { get; private set; }

        private Doctor() : base()
        {
            Patients = new List<Patient>();
            ChatMessages = new List<ChatMessage>();
            TreatmentSchedules = new List<TreatmentSchedule>();
            Certifications = new List<string>();
            Languages = new List<string>();
        }

        public Doctor(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string specialty,
            string photoUrl,
            TimeZoneInfo timeZone,
            List<string> certifications,
            List<string> languages) : this()
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Specialty = specialty;
            PhotoUrl = photoUrl;
            TimeZone = timeZone;
            Certifications = certifications;
            Languages = languages;
            IsOnline = false;
            LastSeen = DateTime.UtcNow;
        }

        public string FullName => $"Dr. {FirstName} {LastName}";

        public void UpdatePersonalInfo(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string specialty,
            string photoUrl,
            TimeZoneInfo timeZone,
            List<string> certifications,
            List<string> languages)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Specialty = specialty;
            PhotoUrl = photoUrl;
            TimeZone = timeZone;
            Certifications = certifications;
            Languages = languages;
            Update();
        }

        public void UpdateOnlineStatus(bool isOnline)
        {
            IsOnline = isOnline;
            LastSeen = DateTime.UtcNow;
            Update();
        }

        public void AddPatient(Patient patient)
        {
            Patients.Add(patient);
            Update();
        }

        public void AddChatMessage(ChatMessage message)
        {
            ChatMessages.Add(message);
            Update();
        }

        public void AddTreatmentSchedule(TreatmentSchedule schedule)
        {
            TreatmentSchedules.Add(schedule);
            Update();
        }
    }
} 