using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects
{
    public class TreatmentSchedule : BaseEntity
    {
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public TreatmentType Type { get; private set; }
        public RecurrencePattern RecurrencePattern { get; private set; } = null!;
        public List<TreatmentTask> Tasks { get; private set; } = new();
        public bool IsCompleted { get; private set; }

        private TreatmentSchedule() : base()
        {
            Tasks = new List<TreatmentTask>();
        }

        public TreatmentSchedule(
            string title,
            string description,
            DateTime startDate,
            DateTime? endDate,
            TreatmentType type,
            RecurrencePattern recurrencePattern) : this()
        {
            Title = title;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;
            RecurrencePattern = recurrencePattern;
            IsCompleted = false;
        }

        public TreatmentSchedule(
            Guid id,
            Guid patientId,
            string title,
            DateTime dueDate,
            bool isDone,
            bool isSkipped) : this()
        {
            Id = id;
            // PatientId is not a property of TreatmentSchedule, may need to adjust
            Title = title;
            StartDate = dueDate;
            IsCompleted = isDone || isSkipped;
        }

        public void AddTask(TreatmentTask task)
        {
            Tasks.Add(task);
            Update();
        }

        public void MarkAsCompleted()
        {
            IsCompleted = true;
            Update();
        }

        public void UpdateSchedule(
            DateTime startDate,
            DateTime? endDate,
            RecurrencePattern recurrencePattern)
        {
            StartDate = startDate;
            EndDate = endDate;
            RecurrencePattern = recurrencePattern;
            Update();
        }
    }

    public class TreatmentTask : BaseEntity
    {
        public string Title { get; private set; } = null!;
        public string Instructions { get; private set; } = null!;
        public DateTime DueDate { get; private set; }
        public bool IsCompleted { get; private set; }
        public DateTime? CompletedDate { get; private set; }

        private TreatmentTask() : base() { }

        public TreatmentTask(
            string title,
            string instructions,
            DateTime dueDate)
        {
            Title = title;
            Instructions = instructions;
            DueDate = dueDate;
            IsCompleted = false;
        }

        public void MarkAsCompleted()
        {
            IsCompleted = true;
            CompletedDate = DateTime.UtcNow;
            Update();
        }
    }

    public enum TreatmentType
    {
        Medication,
        Vitamin,
        Shampoo,
        Massage,
        Exercise,
        PhotoReport,
        Other
    }

    public class RecurrencePattern
    {
        public RecurrenceType Type { get; private set; }
        public int Interval { get; private set; }
        public List<DayOfWeek> DaysOfWeek { get; private set; }
        public TimeSpan TimeOfDay { get; private set; }

        private RecurrencePattern() 
        {
            DaysOfWeek = new List<DayOfWeek>();
        }

        public RecurrencePattern(
            RecurrenceType type,
            int interval,
            List<DayOfWeek> daysOfWeek,
            TimeSpan timeOfDay) : this()
        {
            Type = type;
            Interval = interval;
            DaysOfWeek = daysOfWeek;
            TimeOfDay = timeOfDay;
        }
    }

    public enum RecurrenceType
    {
        Daily,
        Weekly,
        Monthly,
        Custom
    }
} 