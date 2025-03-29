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
        private List<CalendarEvent> _selectedDayEvents = new List<CalendarEvent>();
        
        // Событие для явного уведомления об изменении месяца
        public event EventHandler CurrentMonthChanged;

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set 
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    // Обновляем строковое представление месяца
                    OnPropertyChanged(nameof(CurrentMonthYear));
                    
                    // Явно уведомляем о смене месяца
                    CurrentMonthChanged?.Invoke(this, EventArgs.Empty);
                    
                    System.Diagnostics.Debug.WriteLine($"Месяц изменен на: {CurrentMonthYear}");
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
                // Защищаемся от null
                var newValue = value ?? new List<CalendarEvent>();
                if (SetProperty(ref _selectedDayEvents, newValue))
                {
                    OnPropertyChanged(nameof(HasSelectedDayEvents));
                }
            }
        }
        
        // Обновленное свойство с проверкой на null
        public bool HasSelectedDayEvents => _selectedDayEvents != null && _selectedDayEvents.Any();

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand DaySelectedCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand BackToMonthViewCommand { get; }

        public SimpleCalendarViewModel()
        {
            Title = "Calendar";
            
            // Устанавливаем текущую дату
            var now = DateTime.Now;
            CurrentMonthDate = new DateTime(now.Year, now.Month, 1); // Первый день текущего месяца
            SelectedDate = now;
            
            // SelectedDayEvents уже инициализирован в поле

            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            NextMonthCommand = new Command(ExecuteNextMonth);
            DaySelectedCommand = new Command<string>(ExecuteDaySelected);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            BackToMonthViewCommand = new Command(ExecuteBackToMonthView);
            
            try
            {
                // Загружаем события для текущего дня
                LoadEventsForSelectedDay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке первоначальных событий: {ex.Message}");
                // Уже инициализировано пустым списком, поэтому ничего не делаем
            }
            
            System.Diagnostics.Debug.WriteLine($"SimpleCalendarViewModel инициализирован, текущий месяц: {CurrentMonthYear}");
        }

        private void ExecutePreviousMonth()
        {
            try
            {
                // Переход к предыдущему месяцу
                CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
                System.Diagnostics.Debug.WriteLine($"Переход к предыдущему месяцу: {CurrentMonthYear}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при переходе к предыдущему месяцу: {ex.Message}");
            }
        }

        private void ExecuteNextMonth()
        {
            try
            {
                // Переход к следующему месяцу
                CurrentMonthDate = CurrentMonthDate.AddMonths(1);
                System.Diagnostics.Debug.WriteLine($"Переход к следующему месяцу: {CurrentMonthYear}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при переходе к следующему месяцу: {ex.Message}");
            }
        }

        private void ExecuteDaySelected(string dayString)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteDaySelected: {dayString}");
                
                if (string.IsNullOrEmpty(dayString) || !int.TryParse(dayString, out int day))
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось преобразовать день '{dayString}' в число");
                    return;
                }
                
                // Проверяем валидность дня для текущего месяца
                int daysInMonth = DateTime.DaysInMonth(CurrentMonthDate.Year, CurrentMonthDate.Month);
                if (day < 1 || day > daysInMonth)
                {
                    System.Diagnostics.Debug.WriteLine($"День {day} находится вне диапазона для месяца ({1}-{daysInMonth})");
                    return;
                }
                
                // Создаем дату из текущего месяца и выбранного дня
                var newDate = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, day);
                SelectedDate = newDate;
                
                System.Diagnostics.Debug.WriteLine($"Выбрана дата: {SelectedDate:yyyy-MM-dd}");
                
                LoadEventsForSelectedDay();
                IsMonthViewVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при выборе дня: {ex.Message}");
            }
        }

        private void ExecuteGoToToday()
        {
            try
            {
                var today = DateTime.Today;
                
                // Если месяц другой, переключаем на месяц с сегодняшним днем
                if (CurrentMonthDate.Year != today.Year || CurrentMonthDate.Month != today.Month)
                {
                    CurrentMonthDate = new DateTime(today.Year, today.Month, 1);
                }
                
                SelectedDate = today;
                LoadEventsForSelectedDay();
                
                System.Diagnostics.Debug.WriteLine($"Переход к сегодняшнему дню: {today:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при переходе к сегодняшнему дню: {ex.Message}");
            }
        }
        
        private void ExecuteBackToMonthView()
        {
            IsMonthViewVisible = true;
            System.Diagnostics.Debug.WriteLine("Возврат к месячному виду");
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
                SelectedDayEvents = events; // Здесь уже есть защита от null в сеттере
                
                System.Diagnostics.Debug.WriteLine($"SimpleCalendarViewModel: Загрузка событий завершена");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА SimpleCalendarViewModel: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Создаём пустой список в случае ошибки и устанавливаем его
                SelectedDayEvents = new List<CalendarEvent>();
                
                // Не пробрасываем исключение дальше, а обрабатываем локально
                // Можно добавить уведомление пользователя при необходимости
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
                EventType.MedicationTreatment => "Прием лекарства",
                EventType.Photo => "Фотоотчет",
                EventType.CriticalWarning => "Ограничение",
                EventType.VideoInstruction => "Инструкция",
                _ => "Событие"
            };
        }
    }
} 