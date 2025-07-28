using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.ValueObjects;

namespace HairCarePlus.Server.Domain.Entities
{
    public class Patient : BaseEntity
    {
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PhoneNumber { get; private set; } = null!;
        public DateTime DateOfBirth { get; private set; }
        public DateTime SurgeryDate { get; private set; }
        public string PreferredLanguage { get; private set; } = null!;
        public TimeZoneInfo TimeZone { get; private set; } = null!;
        public List<PhotoReport> PhotoReports { get; private set; } = new();
        public List<Notification> Notifications { get; private set; } = new();
        public List<TreatmentSchedule> TreatmentSchedules { get; private set; } = new();
        public List<ChatMessage> ChatMessages { get; private set; } = new();

        private Patient() : base() 
        {
            PhotoReports = new List<PhotoReport>();
            Notifications = new List<Notification>();
            TreatmentSchedules = new List<TreatmentSchedule>();
            ChatMessages = new List<ChatMessage>();
        }

        public Patient(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            DateTime dateOfBirth,
            DateTime surgeryDate,
            string preferredLanguage,
            TimeZoneInfo timeZone) : this()
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            DateOfBirth = dateOfBirth;
            SurgeryDate = surgeryDate;
            PreferredLanguage = preferredLanguage;
            TimeZone = timeZone;
        }

        public void UpdatePersonalInfo(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string preferredLanguage,
            TimeZoneInfo timeZone)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            PreferredLanguage = preferredLanguage;
            TimeZone = timeZone;
            Update();
        }

        public void AddPhotoReport(PhotoReport photoReport)
        {
            PhotoReports.Add(photoReport);
            Update();
        }

        public void AddNotification(Notification notification)
        {
            Notifications.Add(notification);
            Update();
        }

        public void AddTreatmentSchedule(TreatmentSchedule schedule)
        {
            TreatmentSchedules.Add(schedule);
            Update();
        }

        public void AddChatMessage(ChatMessage message)
        {
            ChatMessages.Add(message);
            Update();
        }
    }
} 