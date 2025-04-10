using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Text;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _isSelected;
        private bool _isToday;
        private bool _hasMedicationEvents;
        private bool _hasPhotoEvents;
        private bool _hasVideoEvents;
        private bool _hasRestrictionEvents;
        private ObservableCollection<CalendarEvent> _events = new();
        private readonly ILogger<CalendarDayViewModel>? _logger;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                if (_isCurrentMonth != value)
                {
                    _isCurrentMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsToday
        {
            get => _isToday;
            set
            {
                if (_isToday != value)
                {
                    _isToday = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasMedicationEvents
        {
            get => _hasMedicationEvents;
            set
            {
                if (_hasMedicationEvents != value)
                {
                    _hasMedicationEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasPhotoEvents
        {
            get => _hasPhotoEvents;
            set
            {
                if (_hasPhotoEvents != value)
                {
                    _hasPhotoEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasVideoEvents
        {
            get => _hasVideoEvents;
            set
            {
                if (_hasVideoEvents != value)
                {
                    _hasVideoEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasRestrictionEvents
        {
            get => _hasRestrictionEvents;
            set
            {
                if (_hasRestrictionEvents != value)
                {
                    _hasRestrictionEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CalendarEvent> Events
        {
            get => _events;
            set
            {
                if (_events != value)
                {
                    _events = value;
                    OnPropertyChanged();
                    UpdateEventProperties();
                }
            }
        }

        public bool HasEvents => Events.Count > 0;
        public bool HasExcessEvents => Events.Count > 3;
        public int ExcessEventsCount => Events.Count > 3 ? Events.Count - 3 : 0;
        public int DayNumber => Date.Day;
        public string DayText => Date.Day.ToString();

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateEventProperties()
        {
            HasMedicationEvents = Events.Any(e => e.EventType == EventType.MedicationTreatment);
            HasPhotoEvents = Events.Any(e => e.EventType == EventType.Photo);
            HasVideoEvents = Events.Any(e => e.EventType == EventType.Video);
            HasRestrictionEvents = Events.Any(e => e.EventType == EventType.CriticalWarning);
            OnPropertyChanged(nameof(HasEvents));
            OnPropertyChanged(nameof(HasExcessEvents));
            OnPropertyChanged(nameof(ExcessEventsCount));
        }
    }
} 