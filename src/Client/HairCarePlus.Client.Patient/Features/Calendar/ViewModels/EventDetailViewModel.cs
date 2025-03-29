using System;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using Microsoft.Maui.Controls;

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

        public async Task LoadEventAsync(int eventId)
        {
            try
            {
                IsBusy = true;
                
                // В реальном приложении здесь был бы вызов метода для получения события по ID
                // Например:
                // Event = await _calendarService.GetEventByIdAsync(eventId);
                
                // Для демонстрации используем временную заглушку
                var events = await _calendarService.GetEventsForDateAsync(DateTime.Today);
                Event = events.FirstOrDefault(e => e.Id == eventId) ?? new CalendarEvent
                {
                    Id = eventId,
                    Title = "Sample Event",
                    Description = "This is a sample event for demonstration purposes.",
                    Date = DateTime.Today,
                    EventType = EventType.MedicationTreatment,
                    TimeOfDay = TimeOfDay.Morning,
                    Priority = EventPriority.Normal
                };
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь был бы код логирования ошибки
                Console.WriteLine($"Error loading event: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load event details.", "OK");
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
                Event.IsCompleted = !Event.IsCompleted;
                await _calendarService.MarkEventAsCompletedAsync(Event.Id, Event.IsCompleted);
                
                // Обновляем свойство для уведомления UI
                OnPropertyChanged(nameof(Event));
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь был бы код логирования ошибки
                Console.WriteLine($"Error toggling completion: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to update event status.", "OK");
            }
        }

        private async Task PostponeAsync()
        {
            if (Event == null)
                return;

            try
            {
                string action = await Application.Current.MainPage.DisplayActionSheet(
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
                
                await Application.Current.MainPage.DisplayAlert("Success", $"Event postponed: {action}", "OK");
                
                // Возвращаемся на предыдущую страницу
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь был бы код логирования ошибки
                Console.WriteLine($"Error postponing event: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to postpone event.", "OK");
            }
        }
    }
} 