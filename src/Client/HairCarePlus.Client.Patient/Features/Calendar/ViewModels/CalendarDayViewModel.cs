using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _hasEvents;
        private bool _isSelected;
        private bool _isToday;

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

        public int DayNumber => Date.Day;
        public string DayText => Date.Day.ToString();

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
} 