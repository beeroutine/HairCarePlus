using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    // Shared view models to avoid duplicate partial class definitions
    
    public partial class MedicationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string instructions;

        [ObservableProperty]
        private string dosage;

        [ObservableProperty]
        private int timesPerDay;

        [ObservableProperty]
        private bool isOptional;
    }

    public partial class RestrictionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string reason;

        [ObservableProperty]
        private bool isCritical;

        [ObservableProperty]
        private string recommendedAlternative;
    }

    public partial class InstructionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;
        
        [ObservableProperty]
        private string description;
        
        [ObservableProperty]
        private string[] steps;
    }
    
    public partial class WarningViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;
        
        [ObservableProperty]
        private string description;
    }

    public partial class CalendarEventViewModel : ObservableObject
    {
        // Using manually implemented properties for better compatibility with the linter
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _type;
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
        
        // Add DayNumber property for compatibility with CalendarViewModel.cs
        private int _dayNumber;
        public int DayNumber
        {
            get => _dayNumber;
            set => SetProperty(ref _dayNumber, value);
        }
        
        // Properties for multi-day events
        private bool _isMultiDay;
        public bool IsMultiDay
        {
            get => _isMultiDay;
            set => SetProperty(ref _isMultiDay, value);
        }
        
        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }
        
        private string _dateRangeText;
        public string DateRangeText
        {
            get => _dateRangeText;
            set => SetProperty(ref _dateRangeText, value);
        }
        
        private double _progressPercentage;
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }
        
        // Helper method to calculate progress for multi-day events
        public void UpdateProgressInfo()
        {
            if (!IsMultiDay) return;
            
            // Format date range text
            DateRangeText = $"{Date:d MMM} - {EndDate:d MMM}";
            
            // Calculate progress percentage
            if (Date < EndDate)
            {
                var totalDays = (EndDate - Date).TotalDays;
                var daysElapsed = (DateTime.Today - Date).TotalDays;
                
                if (daysElapsed < 0)
                {
                    // Event hasn't started yet
                    ProgressPercentage = 0;
                }
                else if (daysElapsed >= totalDays)
                {
                    // Event is complete
                    ProgressPercentage = 100;
                }
                else
                {
                    // Event is in progress
                    ProgressPercentage = Math.Max(0, Math.Min(100, (daysElapsed / totalDays) * 100));
                }
            }
            else
            {
                ProgressPercentage = 100;
            }
        }
    }
} 