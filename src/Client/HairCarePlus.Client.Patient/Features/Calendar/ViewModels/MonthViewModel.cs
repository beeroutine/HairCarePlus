using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class MonthViewModel : ObservableObject
    {
        private readonly ICalendarService _calendarService;

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<CalendarDayViewModel> days = new();

        [ObservableProperty]
        private string monthTitle;

        public MonthViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadMonth(DateTime.Today);
        }

        [RelayCommand]
        private void PreviousMonth()
        {
            LoadMonth(SelectedDate.AddMonths(-1));
        }

        [RelayCommand]
        private void NextMonth()
        {
            LoadMonth(SelectedDate.AddMonths(1));
        }

        private void LoadMonth(DateTime date)
        {
            SelectedDate = date;
            MonthTitle = date.ToString("MMMM yyyy");
            Days.Clear();

            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Добавляем дни предыдущего месяца для заполнения первой недели
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            if (firstDayOfWeek != 0) // Воскресенье = 0
            {
                for (int i = firstDayOfWeek - 1; i >= 0; i--)
                {
                    var day = firstDayOfMonth.AddDays(-i - 1);
                    Days.Add(new CalendarDayViewModel
                    {
                        Date = day,
                        IsCurrentMonth = false,
                        Events = new ObservableCollection<CalendarEventViewModel>()
                    });
                }
            }

            // Добавляем дни текущего месяца
            for (var day = firstDayOfMonth; day <= lastDayOfMonth; day = day.AddDays(1))
            {
                var events = new ObservableCollection<CalendarEventViewModel>();
                // TODO: Загрузить события для этого дня
                
                Days.Add(new CalendarDayViewModel
                {
                    Date = day,
                    IsCurrentMonth = true,
                    IsToday = day.Date == DateTime.Today,
                    Events = events
                });
            }

            // Добавляем дни следующего месяца для заполнения последней недели
            var lastDayOfWeek = (int)lastDayOfMonth.DayOfWeek;
            if (lastDayOfWeek != 6) // Суббота = 6
            {
                for (int i = 1; i <= 6 - lastDayOfWeek; i++)
                {
                    var day = lastDayOfMonth.AddDays(i);
                    Days.Add(new CalendarDayViewModel
                    {
                        Date = day,
                        IsCurrentMonth = false,
                        Events = new ObservableCollection<CalendarEventViewModel>()
                    });
                }
            }
        }
    }

    public partial class CalendarDayViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime date;

        [ObservableProperty]
        private bool isCurrentMonth;

        [ObservableProperty]
        private bool isToday;

        [ObservableProperty]
        private ObservableCollection<CalendarEventViewModel> events;
    }
} 