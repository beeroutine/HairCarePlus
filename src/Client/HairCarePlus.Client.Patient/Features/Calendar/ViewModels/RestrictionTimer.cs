using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class RestrictionTimer : INotifyPropertyChanged
    {
        private CalendarEvent _restrictionEvent;
        private string _remainingTimeText;
        private double _progressPercentage;

        public event PropertyChangedEventHandler PropertyChanged;

        public CalendarEvent RestrictionEvent
        {
            get => _restrictionEvent;
            set 
            {
                if (_restrictionEvent != value)
                {
                    _restrictionEvent = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set 
            {
                if (_remainingTimeText != value)
                {
                    _remainingTimeText = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set 
            {
                if (_progressPercentage != value)
                {
                    _progressPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title => RestrictionEvent?.Title;
        public string Description => RestrictionEvent?.Description;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 