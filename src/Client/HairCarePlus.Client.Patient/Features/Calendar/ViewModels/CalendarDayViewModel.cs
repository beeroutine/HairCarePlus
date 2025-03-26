using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _hasEvents;
        private bool _isSelected;
        private bool _isToday;
        private bool _hasMedication;
        private bool _hasPhoto;
        private bool _hasRestriction;
        private bool _hasInstruction;
        private int _totalEvents;

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

        public bool HasEvents
        {
            get => _hasEvents;
            set
            {
                if (_hasEvents != value)
                {
                    _hasEvents = value;
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

        public bool HasMedication
        {
            get => _hasMedication;
            set
            {
                if (_hasMedication != value)
                {
                    _hasMedication = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasPhoto
        {
            get => _hasPhoto;
            set
            {
                if (_hasPhoto != value)
                {
                    _hasPhoto = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasRestriction
        {
            get => _hasRestriction;
            set
            {
                if (_hasRestriction != value)
                {
                    _hasRestriction = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasInstruction
        {
            get => _hasInstruction;
            set
            {
                if (_hasInstruction != value)
                {
                    _hasInstruction = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalEvents
        {
            get => _totalEvents;
            set
            {
                if (_totalEvents != value)
                {
                    _totalEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasExcessEvents => TotalEvents > 3;
        public int ExcessEventsCount => TotalEvents > 3 ? TotalEvents - 3 : 0;
        public int DayNumber => Date.Day;
        public string DayText => Date.Day.ToString();

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
} 