using System;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;
using Microsoft.Maui.Controls;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class EventDetailViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private CalendarEvent _event;

        public EventDetailViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            Title = "Event Details";
            
            ToggleCompletionCommand = new Command(async () => await ToggleCompletionAsync());
            PostponeCommand = new Command(async () => await PostponeAsync());
        }

        public CalendarEvent Event
        {
            get => _event;
            set => SetProperty(ref _event, value);
        }

        public ICommand ToggleCompletionCommand { get; }
        public ICommand PostponeCommand { get; }

        public async Task LoadEventAsync(Guid id)
        {
            try
            {
                IsBusy = true;
                
                // В реальном приложении здесь был бы вызов метода для получения события по ID
                // Например:
                // Event = await _calendarService.GetEventByIdAsync(eventId);
                
                // Для демонстрации используем временную заглушку
                var events = await _calendarService.GetEventsForDateAsync(DateTime.Today);
                Event = events.FirstOrDefault(e => e.Id == id) ?? new CalendarEvent
                {
                    Id = id,
                    Title = "Sample Event",
                    Description = "This is a sample event for demonstration purposes.",
                    Date = DateTime.Today,
                    EventType = EventType.MedicationTreatment,
                    TimeOfDay = TimeOfDay.Morning,
                    Priority = EventPriority.Normal
                };
            }
            catch (Exception)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Error", "Failed to load event details.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ToggleCompletionAsync()
        {
            if (Event == null)
                return;

            try
            {
                var originalIsCompleted = Event.IsCompleted;
                Event.IsCompleted = !Event.IsCompleted;
                
                var success = await _calendarService.MarkEventAsCompletedAsync(Event.Id);
                
                if (!success)
                {
                    Event.IsCompleted = originalIsCompleted; // Revert on failure
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.DisplayAlert("Error", "Failed to update event status.", "OK");
                    });
                    return;
                }
                
                // Notify UI of the change
                OnPropertyChanged(nameof(Event));
            }
            catch (Exception)
            {
                Event.IsCompleted = !Event.IsCompleted; // Revert the change
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to update event status.", "OK");
                });
            }
        }

        private async Task PostponeAsync()
        {
            if (Event == null)
                return;

            try
            {
                string action = await MauiApp.Current.MainPage.DisplayActionSheet(
                    "Postpone Event", 
                    "Cancel", 
                    null,
                    "Postpone 1 hour", 
                    "Postpone to this evening", 
                    "Postpone to tomorrow", 
                    "Postpone to next week");

                if (action == "Cancel" || string.IsNullOrEmpty(action))
                    return;

                // В реальном приложении здесь был бы код для изменения даты события
                // Например:
                switch (action)
                {
                    case "Postpone 1 hour":
                        // Event.Date = Event.Date.AddHours(1);
                        break;
                    case "Postpone to this evening":
                        // Event.Date = Event.Date.Date.AddHours(18);
                        // Event.TimeOfDay = TimeOfDay.Evening;
                        break;
                    case "Postpone to tomorrow":
                        // Event.Date = Event.Date.Date.AddDays(1);
                        break;
                    case "Postpone to next week":
                        // Event.Date = Event.Date.Date.AddDays(7);
                        break;
                }

                // После изменения даты сохраняем изменения
                // await _calendarService.UpdateEventAsync(Event);
                
                await MauiApp.Current.MainPage.DisplayAlert("Success", $"Event postponed: {action}", "OK");
                
                // Возвращаемся на предыдущую страницу
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Error", "Failed to postpone event.", "OK");
            }
        }
    }
} 