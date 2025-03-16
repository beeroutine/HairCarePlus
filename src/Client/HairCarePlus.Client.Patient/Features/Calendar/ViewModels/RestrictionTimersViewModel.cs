using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class RestrictionTimer : BaseViewModel
    {
        private CalendarEvent _restrictionEvent;
        private string _remainingTimeText;
        private double _progressPercentage;

        public CalendarEvent RestrictionEvent
        {
            get => _restrictionEvent;
            set => SetProperty(ref _restrictionEvent, value);
        }

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set => SetProperty(ref _remainingTimeText, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string Title => RestrictionEvent?.Title;
        public string Description => RestrictionEvent?.Description;
    }

    public class RestrictionTimersViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private ObservableCollection<RestrictionTimer> _activeRestrictions = new ObservableCollection<RestrictionTimer>();
        private Timer _updateTimer;

        public ObservableCollection<RestrictionTimer> ActiveRestrictions
        {
            get => _activeRestrictions;
            set => SetProperty(ref _activeRestrictions, value);
        }

        public RestrictionTimersViewModel(ICalendarService calendarService)
        {
            Title = "Restrictions";
            _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));

            // Start the timer to update countdown
            _updateTimer = new Timer(UpdateTimers, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            // Initial loading of restrictions
            Task.Run(LoadRestrictionsAsync);
        }

        private async Task LoadRestrictionsAsync()
        {
            try
            {
                IsBusy = true;
                
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                
                ActiveRestrictions.Clear();
                foreach (var restriction in restrictions.Where(r => r.ExpirationDate.HasValue))
                {
                    var timer = new RestrictionTimer
                    {
                        RestrictionEvent = restriction
                    };
                    ActiveRestrictions.Add(timer);
                }
                
                // Initial update of timer texts
                UpdateTimerTexts();
            }
            catch (Exception)
            {
                // In a real app, handle the error appropriately
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateTimers(object state)
        {
            // This method is called on a timer thread, so we need to dispatch to the UI thread
            MainThread.BeginInvokeOnMainThread(UpdateTimerTexts);
        }

        private void UpdateTimerTexts()
        {
            var now = DateTime.Now;
            var timersToRemove = new List<RestrictionTimer>();
            
            foreach (var timer in ActiveRestrictions)
            {
                if (timer.RestrictionEvent.ExpirationDate.HasValue)
                {
                    var expirationDate = timer.RestrictionEvent.ExpirationDate.Value;
                    
                    if (expirationDate <= now)
                    {
                        // This restriction has expired, mark for removal
                        timersToRemove.Add(timer);
                    }
                    else
                    {
                        // Calculate remaining time
                        var timeSpan = expirationDate - now;
                        
                        // Format the remaining time text
                        timer.RemainingTimeText = FormatTimeSpan(timeSpan);
                        
                        // Calculate progress (from event date to expiration date)
                        var totalDuration = expirationDate - timer.RestrictionEvent.Date;
                        var elapsed = now - timer.RestrictionEvent.Date;
                        timer.ProgressPercentage = Math.Min(100, Math.Max(0, (elapsed.TotalMilliseconds / totalDuration.TotalMilliseconds) * 100));
                    }
                }
            }
            
            // Remove expired timers
            foreach (var timer in timersToRemove)
            {
                ActiveRestrictions.Remove(timer);
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 1)
            {
                return $"{(int)timeSpan.TotalDays} days";
            }
            else if (timeSpan.TotalHours > 1)
            {
                return $"{(int)timeSpan.TotalHours} hours";
            }
            else
            {
                return $"{(int)timeSpan.TotalMinutes} minutes";
            }
        }

        // Cleanup when the ViewModel is no longer needed
        public void Dispose()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }
    }
} 