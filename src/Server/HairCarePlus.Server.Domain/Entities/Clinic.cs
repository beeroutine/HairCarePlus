using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.ValueObjects;

namespace HairCarePlus.Server.Domain.Entities
{
    public class Clinic : BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Address { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Email { get; private set; }
        public string Website { get; private set; }
        public TimeZoneInfo TimeZone { get; private set; }
        public List<ClinicStaff> Staff { get; private set; }
        public List<Patient> Patients { get; private set; }
        public List<WorkingHours> WorkingHours { get; private set; }
        public ClinicSettings Settings { get; private set; }

        private Clinic() : base()
        {
            Staff = new List<ClinicStaff>();
            Patients = new List<Patient>();
            WorkingHours = new List<WorkingHours>();
        }

        public Clinic(
            string name,
            string description,
            string address,
            string phoneNumber,
            string email,
            string website,
            TimeZoneInfo timeZone,
            ClinicSettings settings) : this()
        {
            Name = name;
            Description = description;
            Address = address;
            PhoneNumber = phoneNumber;
            Email = email;
            Website = website;
            TimeZone = timeZone;
            Settings = settings;
        }

        public void UpdateInfo(
            string name,
            string description,
            string address,
            string phoneNumber,
            string email,
            string website,
            TimeZoneInfo timeZone)
        {
            Name = name;
            Description = description;
            Address = address;
            PhoneNumber = phoneNumber;
            Email = email;
            Website = website;
            TimeZone = timeZone;
            Update();
        }

        public void AddStaffMember(ClinicStaff staff)
        {
            Staff.Add(staff);
            Update();
        }

        public void AddPatient(Patient patient)
        {
            Patients.Add(patient);
            Update();
        }

        public void UpdateWorkingHours(List<WorkingHours> workingHours)
        {
            WorkingHours = workingHours;
            Update();
        }

        public void UpdateSettings(ClinicSettings settings)
        {
            Settings = settings;
            Update();
        }
    }

    public class ClinicStaff : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public StaffRole Role { get; private set; }
        public List<string> Specializations { get; private set; }
        public bool IsActive { get; private set; }

        private ClinicStaff() : base()
        {
            Specializations = new List<string>();
        }

        public ClinicStaff(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            StaffRole role,
            List<string> specializations) : this()
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Role = role;
            Specializations = specializations;
            IsActive = true;
        }

        public void UpdateInfo(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            StaffRole role,
            List<string> specializations)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Role = role;
            Specializations = specializations;
            Update();
        }

        public void Deactivate()
        {
            IsActive = false;
            Update();
        }

        public void Activate()
        {
            IsActive = true;
            Update();
        }
    }

    public class WorkingHours
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public TimeSpan OpenTime { get; private set; }
        public TimeSpan CloseTime { get; private set; }
        public bool IsClosed { get; private set; }

        private WorkingHours() { }

        public WorkingHours(
            DayOfWeek dayOfWeek,
            TimeSpan openTime,
            TimeSpan closeTime,
            bool isClosed)
        {
            DayOfWeek = dayOfWeek;
            OpenTime = openTime;
            CloseTime = closeTime;
            IsClosed = isClosed;
        }
    }

    public class ClinicSettings
    {
        public List<string> SupportedLanguages { get; private set; }
        public string DefaultLanguage { get; private set; }
        public NotificationSettings NotificationSettings { get; private set; }
        public AISettings AISettings { get; private set; }

        private ClinicSettings()
        {
            SupportedLanguages = new List<string>();
        }

        public ClinicSettings(
            List<string> supportedLanguages,
            string defaultLanguage,
            NotificationSettings notificationSettings,
            AISettings aiSettings) : this()
        {
            SupportedLanguages = supportedLanguages;
            DefaultLanguage = defaultLanguage;
            NotificationSettings = notificationSettings;
            AISettings = aiSettings;
        }
    }

    public class NotificationSettings
    {
        public bool EnableEmailNotifications { get; private set; }
        public bool EnableSMSNotifications { get; private set; }
        public bool EnablePushNotifications { get; private set; }
        public int ReminderLeadTime { get; private set; }

        public NotificationSettings(
            bool enableEmailNotifications,
            bool enableSMSNotifications,
            bool enablePushNotifications,
            int reminderLeadTime)
        {
            EnableEmailNotifications = enableEmailNotifications;
            EnableSMSNotifications = enableSMSNotifications;
            EnablePushNotifications = enablePushNotifications;
            ReminderLeadTime = reminderLeadTime;
        }
    }

    public class AISettings
    {
        public bool EnableAutoTranslation { get; private set; }
        public bool EnablePhotoAnalysis { get; private set; }
        public List<string> PreferredAIServices { get; private set; }

        private AISettings()
        {
            PreferredAIServices = new List<string>();
        }

        public AISettings(
            bool enableAutoTranslation,
            bool enablePhotoAnalysis,
            List<string> preferredAIServices) : this()
        {
            EnableAutoTranslation = enableAutoTranslation;
            EnablePhotoAnalysis = enablePhotoAnalysis;
            PreferredAIServices = preferredAIServices;
        }
    }

    public enum StaffRole
    {
        Administrator,
        Doctor,
        Nurse,
        Receptionist,
        Consultant,
        Other
    }
} 