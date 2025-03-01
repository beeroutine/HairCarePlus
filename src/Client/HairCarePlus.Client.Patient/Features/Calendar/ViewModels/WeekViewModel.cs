using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class WeekViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<WeekDayViewModel> weekDays = new();

        [ObservableProperty]
        private string weekTitle;

        [ObservableProperty]
        private bool isLoading;

        public WeekViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadWeekAsync(DateTime.Today).ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task PreviousWeek()
        {
            await LoadWeekAsync(SelectedDate.AddDays(-7));
        }

        [RelayCommand]
        private async Task NextWeek()
        {
            await LoadWeekAsync(SelectedDate.AddDays(7));
        }

        private async Task LoadWeekAsync(DateTime date)
        {
            try
            {
                IsLoading = true;
                SelectedDate = date;
                
                // Находим понедельник текущей недели
                var monday = date.AddDays(-(int)date.DayOfWeek + 1);
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    monday = monday.AddDays(-7);

                var sunday = monday.AddDays(6);
                WeekTitle = $"{monday:d MMMM} - {sunday:d MMMM}";
                
                WeekDays.Clear();

                // Загружаем дни недели
                for (int i = 0; i < 7; i++)
                {
                    var currentDate = monday.AddDays(i);
                    var dayNumber = (currentDate - _calendarService.GetOperationDate()).Days + 1;
                    
                    // Загружаем все типы событий для текущего дня
                    var events = new ObservableCollection<CalendarEventViewModel>();
                    
                    if (dayNumber > 0)
                    {
                        // Загружаем медикаменты
                        var medications = await _calendarService.GetMedicationsForDayAsync(dayNumber);
                        foreach (var med in medications)
                        {
                            events.Add(new CalendarEventViewModel
                            {
                                Name = med.Name,
                                Description = $"{med.Dosage} - {med.Instructions}",
                                Type = "Medication",
                                Date = currentDate
                            });
                        }

                        // Загружаем ограничения
                        var restrictions = await _calendarService.GetRestrictionsForDayAsync(dayNumber);
                        foreach (var restriction in restrictions)
                        {
                            events.Add(new CalendarEventViewModel
                            {
                                Name = restriction.Name,
                                Description = restriction.Description,
                                Type = "Restriction",
                                Date = currentDate
                            });
                        }

                        // Загружаем инструкции
                        var instructions = await _calendarService.GetInstructionsForDayAsync(dayNumber);
                        foreach (var instruction in instructions)
                        {
                            events.Add(new CalendarEventViewModel
                            {
                                Name = instruction.Name,
                                Description = instruction.Description,
                                Type = "Instruction",
                                Date = currentDate
                            });
                        }

                        // Загружаем предупреждения
                        var warnings = await _calendarService.GetWarningsForDayAsync(dayNumber);
                        foreach (var warning in warnings)
                        {
                            events.Add(new CalendarEventViewModel
                            {
                                Name = warning.Name,
                                Description = warning.Description,
                                Type = "Warning",
                                Date = currentDate
                            });
                        }
                    }
                    
                    WeekDays.Add(new WeekDayViewModel
                    {
                        Date = currentDate,
                        DayOfWeek = currentDate.ToString("ddd"),
                        DayNumber = Math.Max(0, dayNumber),
                        IsToday = currentDate.Date == DateTime.Today,
                        Events = events
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public partial class WeekDayViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime date;

        [ObservableProperty]
        private string dayOfWeek;

        [ObservableProperty]
        private int dayNumber;

        [ObservableProperty]
        private bool isToday;

        [ObservableProperty]
        private ObservableCollection<CalendarEventViewModel> events;
    }
} 