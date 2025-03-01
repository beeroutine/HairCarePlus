using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Client.Patient.Features.Calendar.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class ListViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;

        [ObservableProperty]
        private ObservableCollection<CalendarEventViewModel> events = new();

        public ListViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadEventsAsync().ConfigureAwait(false);
        }

        private async Task LoadEventsAsync()
        {
            Events.Clear();
            var allEvents = await _calendarService.GetEventsAsync();
            
            foreach (var evt in allEvents.OrderBy(e => e.StartDay))
            {
                Events.Add(new CalendarEventViewModel
                {
                    Name = evt.Name,
                    Description = evt.Description,
                    Type = evt.Type.ToString(),
                    Date = _calendarService.GetDateForDay(evt.StartDay),
                    DayNumber = evt.StartDay
                });
            }
        }
    }
} 