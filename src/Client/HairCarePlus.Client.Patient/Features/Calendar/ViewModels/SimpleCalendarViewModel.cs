using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Calendar.Models;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class SimpleCalendarViewModel : BaseViewModel
    {
        private DateTime _currentMonthDate;
        private DateTime _selectedDate;
        private bool _isMonthViewVisible = true;
        private List<CalendarEvent> _selectedDayEvents;
        private bool _hasSelectedDayEvents;
        
        // Событие для явного уведомления об изменении месяца
        public event EventHandler CurrentMonthChanged;

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set 
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    // Явно уведомляем о смене месяца
                    CurrentMonthChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string CurrentMonthYear => CurrentMonthDate.ToString("MMMM yyyy");
        
        public string SelectedDateText => SelectedDate.ToString("d MMMM");

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }
        
        public bool IsMonthViewVisible
        {
            get => _isMonthViewVisible;
            set => SetProperty(ref _isMonthViewVisible, value);
        }
        
        public List<CalendarEvent> SelectedDayEvents
        {
            get => _selectedDayEvents;
            set
            {
                if (SetProperty(ref _selectedDayEvents, value))
                {
                    OnPropertyChanged(nameof(HasSelectedDayEvents));
                }
            }
        }
        
        public bool HasSelectedDayEvents => SelectedDayEvents != null && SelectedDayEvents.Any();

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand DaySelectedCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand BackToMonthViewCommand { get; }

        public SimpleCalendarViewModel()
        {
            Title = "Calendar";
            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;
            SelectedDayEvents = new List<CalendarEvent>();

            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            NextMonthCommand = new Command(ExecuteNextMonth);
            DaySelectedCommand = new Command<DateTime>(ExecuteDaySelected);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            BackToMonthViewCommand = new Command(ExecuteBackToMonthView);
            
            // Загружаем события для текущего дня
            LoadEventsForSelectedDay();
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecuteDaySelected(DateTime date)
        {
            SelectedDate = date;
            LoadEventsForSelectedDay();
            IsMonthViewVisible = false;
        }

        private void ExecuteGoToToday()
        {
            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;
            LoadEventsForSelectedDay();
        }
        
        private void ExecuteBackToMonthView()
        {
            IsMonthViewVisible = true;
        }
        
        // Заглушка для загрузки событий выбранного дня
        // В реальном приложении здесь должен быть вызов сервиса
        private void LoadEventsForSelectedDay()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"SimpleCalendarViewModel: Начало загрузки событий для {SelectedDate:yyyy-MM-dd}");
                
                // TODO: Заменить на реальную загрузку данных из сервиса
                // Имитация загрузки данных
                var events = GenerateMockEvents();
                
                System.Diagnostics.Debug.WriteLine($"SimpleCalendarViewModel: Загружено {events.Count} событий");
                SelectedDayEvents = events;
                
                System.Diagnostics.Debug.WriteLine($"SimpleCalendarViewModel: Загрузка событий завершена");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА SimpleCalendarViewModel: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Создаём пустой список в случае ошибки
                SelectedDayEvents = new List<CalendarEvent>();
                
                // Пробрасываем исключение дальше для возможной обработки
                throw new Exception($"Ошибка загрузки событий в SimpleCalendarViewModel: {ex.Message}", ex);
            }
        }
        
        // Заглушка для генерации тестовых данных
        private List<CalendarEvent> GenerateMockEvents()
        {
            if (new Random().Next(3) == 0) // Имитация пустого дня (примерно для трети дней)
            {
                return new List<CalendarEvent>();
            }
            
            var events = new List<CalendarEvent>();
            var rand = new Random();
            int eventCount = rand.Next(1, 4);
            
            for (int i = 0; i < eventCount; i++)
            {
                var eventType = (EventType)rand.Next(0, 4);
                events.Add(new CalendarEvent
                {
                    Id = i + 1,
                    Title = GetTitleForEventType(eventType),
                    Description = "Описание события " + (i + 1),
                    Date = SelectedDate,
                    EventType = eventType,
                    TimeOfDay = (TimeOfDay)rand.Next(0, 3),
                    IsCompleted = rand.Next(2) == 0
                });
            }
            
            return events;
        }
        
        private string GetTitleForEventType(EventType eventType)
        {
            return eventType switch
            {
                EventType.Medication => "Прием лекарства",
                EventType.Photo => "Фотоотчет",
                EventType.Restriction => "Ограничение",
                EventType.Instruction => "Инструкция",
                _ => "Событие"
            };
        }
    }
} 