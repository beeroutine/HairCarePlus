using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Client.Patient.Features.Calendar.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class DayTodoViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;

        [ObservableProperty]
        private DateTime currentDate = DateTime.Today;

        [ObservableProperty]
        private int dayNumber;

        [ObservableProperty]
        private ObservableCollection<CalendarEventViewModel> events = new();

        public DayTodoViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadEventsAsync().ConfigureAwait(false);
        }

        private async Task LoadEventsAsync()
        {
            Events.Clear();
            
            var operationDate = _calendarService.GetOperationDate();
            DayNumber = (CurrentDate - operationDate).Days + 1;

            if (DayNumber > 0)
            {
                // Загружаем медикаменты
                var medications = await _calendarService.GetMedicationsForDayAsync(DayNumber);
                foreach (var med in medications)
                {
                    Events.Add(new CalendarEventViewModel
                    {
                        Name = med.Name,
                        Description = $"{med.Dosage} - {med.Instructions}",
                        Type = "Medication",
                        Date = CurrentDate,
                        IsCompleted = false
                    });
                }

                // Загружаем ограничения
                var restrictions = await _calendarService.GetRestrictionsForDayAsync(DayNumber);
                foreach (var restriction in restrictions)
                {
                    Events.Add(new CalendarEventViewModel
                    {
                        Name = restriction.Name,
                        Description = restriction.Description,
                        Type = "Restriction",
                        Date = CurrentDate,
                        IsCompleted = false
                    });
                }

                // Загружаем инструкции
                var instructions = await _calendarService.GetInstructionsForDayAsync(DayNumber);
                foreach (var instruction in instructions)
                {
                    Events.Add(new CalendarEventViewModel
                    {
                        Name = instruction.Name,
                        Description = instruction.Description,
                        Type = "Instruction",
                        Date = CurrentDate,
                        IsCompleted = false
                    });
                }

                // Загружаем предупреждения
                var warnings = await _calendarService.GetWarningsForDayAsync(DayNumber);
                foreach (var warning in warnings)
                {
                    Events.Add(new CalendarEventViewModel
                    {
                        Name = warning.Name,
                        Description = warning.Description,
                        Type = "Warning",
                        Date = CurrentDate,
                        IsCompleted = false
                    });
                }
            }
        }
    }
} 