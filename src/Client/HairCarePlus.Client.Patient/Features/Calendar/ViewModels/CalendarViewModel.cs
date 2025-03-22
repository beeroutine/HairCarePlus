using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly INotificationService _notificationService;
        
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private ObservableCollection<CalendarEvent> _eventsForSelectedDate = new ObservableCollection<CalendarEvent>();
        private ObservableCollection<CalendarEvent> _eventsForMonth = new ObservableCollection<CalendarEvent>();
        private ObservableCollection<RestrictionTimer> _activeRestrictions = new ObservableCollection<RestrictionTimer>();
        private ObservableCollection<CalendarDayViewModel> _calendarDays = new ObservableCollection<CalendarDayViewModel>();
        private string _errorMessage;
        private bool _hasError;
        private int _retryCount;
        private const int MaxRetries = 3;
        private string _bottomSheetTitle;
        private bool _isPopupVisible = false;

        public ObservableCollection<CalendarDayViewModel> CalendarDays
        {
            get => _calendarDays;
            private set => SetProperty(ref _calendarDays, value);
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand GoToTodayCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }
        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand MarkEventCompletedCommand { get; private set; }
        public ICommand DaySelectedCommand { get; private set; }
        public ICommand ClosePopupCommand { get; private set; }

        public ObservableCollection<RestrictionTimer> ActiveRestrictions
        {
            get => _activeRestrictions;
            private set => SetProperty(ref _activeRestrictions, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    HasError = !string.IsNullOrEmpty(value);
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set => SetProperty(ref _isPopupVisible, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    LoadEventsForSelectedDateAsync().ConfigureAwait(false);
                    UpdateCalendarDays();
                    _bottomSheetTitle = value.ToString("MMMM d, yyyy");
                    OnPropertyChanged(nameof(BottomSheetTitle));
                }
            }
        }

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    LoadEventsForMonthAsync().ConfigureAwait(false);
                    UpdateCalendarDays();
                }
            }
        }

        public ObservableCollection<CalendarEvent> EventsForSelectedDate
        {
            get => _eventsForSelectedDate;
            set => SetProperty(ref _eventsForSelectedDate, value);
        }

        public ObservableCollection<CalendarEvent> EventsForMonth
        {
            get => _eventsForMonth;
            set
            {
                if (SetProperty(ref _eventsForMonth, value))
                {
                    UpdateCalendarDays();
                }
            }
        }

        public string BottomSheetTitle 
        {
            get => _bottomSheetTitle;
            private set => SetProperty(ref _bottomSheetTitle, value);
        }

        public CalendarViewModel()
        {
            Title = "Calendar";
            InitializeCommands();
            
            // Добавляем тестовые данные для демонстрации
            AddSampleEvents();
            
            UpdateCalendarDays();
        }

        public CalendarViewModel(ICalendarService calendarService, INotificationService notificationService) : this()
        {
            _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            Task.Run(async () =>
            {
                await RefreshDataAsync();
            });
        }

        private void UpdateCalendarDays()
        {
            try
            {
                var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
                var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                
                // Create a new collection outside the UI thread
                var days = new List<CalendarDayViewModel>(42); // Pre-allocate capacity for better performance
                
                // Cache for faster lookups
                var eventLookup = new Dictionary<DateTime, bool>();
                foreach (var evt in EventsForMonth)
                {
                    var date = evt.Date.Date;
                    eventLookup[date] = true;
                }
                
                // Generate all days for the calendar grid (6 rows x 7 columns = 42 days)
                for (int i = 0; i < 42; i++)
                {
                    var currentDate = firstDayOfCalendarView.AddDays(i);
                    var isCurrentMonth = currentDate.Month == CurrentMonthDate.Month;
                    
                    // Check for events using the lookup dictionary (much faster than LINQ)
                    var hasEvents = eventLookup.ContainsKey(currentDate.Date);
                    
                    // Create day view model
                    days.Add(new CalendarDayViewModel
                    {
                        Date = currentDate,
                        IsCurrentMonth = isCurrentMonth,
                        HasEvents = hasEvents,
                        IsSelected = currentDate.Date == SelectedDate.Date
                    });
                }
                
                // Update collection on UI thread with a single operation
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    // Replace the entire collection at once to minimize UI updates
                    CalendarDays = new ObservableCollection<CalendarDayViewModel>(days);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating calendar days: {ex.Message}");
            }
        }

        private void InitializeCommands()
        {
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            GoToTodayCommand = new Command(ExecuteGoToToday);
            NextMonthCommand = new Command(ExecuteNextMonth);
            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            MarkEventCompletedCommand = new Command<CalendarEvent>(ExecuteMarkEventCompleted);
            DaySelectedCommand = new Command<DateTime>(ShowPopupForDay);
            ClosePopupCommand = new Command(ExecuteClosePopup);
        }

        private async Task RefreshDataAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                _retryCount = 0;

                await LoadEventsForSelectedDateAsync();
                await LoadEventsForMonthAsync();
                await LoadActiveRestrictionsAsync();
            }
            catch (Exception ex)
            {
                HandleError("Error refreshing calendar data", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteGoToToday()
        {
            SelectedDate = DateTime.Today;
            CurrentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private async void ExecuteMarkEventCompleted(CalendarEvent calendarEvent)
        {
            if (calendarEvent == null)
                return;

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                await _calendarService.MarkEventAsCompletedAsync(calendarEvent.Id, !calendarEvent.IsCompleted);
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                HandleError("Error updating event status", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEventsForSelectedDateAsync()
        {
            try
            {
                if (_calendarService != null)
                {
                    // Загружаем данные с сервера, если сервис доступен
                    var events = await _calendarService.GetEventsForDateAsync(SelectedDate);
                    EventsForSelectedDate.Clear();
                    foreach (var calendarEvent in events)
                    {
                        EventsForSelectedDate.Add(calendarEvent);
                    }
                }
                else
                {
                    // В демо-режиме используем события из EventsForMonth
                    EventsForSelectedDate.Clear();
                    foreach (var calendarEvent in EventsForMonth.Where(e => e.Date.Date == SelectedDate.Date))
                    {
                        EventsForSelectedDate.Add(calendarEvent);
                    }
                }
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                if (await HandleErrorWithRetryAsync("Error loading events", ex))
                {
                    await LoadEventsForSelectedDateAsync();
                }
            }
        }

        private async Task LoadEventsForMonthAsync()
        {
            try
            {
                // Get the first and last day of the month
                var firstDayOfMonth = new DateTime(CurrentMonthDate.Year, CurrentMonthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                // We also need to include the days from previous/next month that appear in the calendar view
                var firstDayOfCalendarView = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
                var lastDayOfCalendarView = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek);
                
                // Get events for the entire calendar view
                var events = await _calendarService.GetEventsForDateRangeAsync(firstDayOfCalendarView, lastDayOfCalendarView);
                
                EventsForMonth.Clear();
                foreach (var calendarEvent in events)
                {
                    EventsForMonth.Add(calendarEvent);
                }
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                if (await HandleErrorWithRetryAsync("Error loading calendar", ex))
                {
                    await LoadEventsForMonthAsync();
                }
            }
        }

        private async Task LoadActiveRestrictionsAsync()
        {
            try
            {
                var restrictions = await _calendarService.GetActiveRestrictionsAsync();
                ActiveRestrictions.Clear();
                foreach (var restriction in restrictions)
                {
                    ActiveRestrictions.Add(new RestrictionTimer { RestrictionEvent = restriction });
                }
            }
            catch (Exception ex)
            {
                if (await HandleErrorWithRetryAsync("Error loading restrictions", ex))
                {
                    await LoadActiveRestrictionsAsync();
                }
            }
        }

        private void HandleError(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
            ErrorMessage = $"{message}. Please try again.";
        }

        private async Task<bool> HandleErrorWithRetryAsync(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");

            if (_retryCount < MaxRetries)
            {
                _retryCount++;
                ErrorMessage = $"{message}. Retrying... (Attempt {_retryCount}/{MaxRetries})";
                await Task.Delay(1000 * _retryCount); // Exponential backoff
                return true;
            }

            ErrorMessage = $"{message}. Please try again.";
            return false;
        }

        private void AddSampleEvents()
        {
            try
            {
                // Создаем несколько событий для текущего месяца
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                
                // Ограничение на 5 дней от начала месяца
                var restriction = new CalendarEvent
                {
                    Id = 1,
                    Title = "Не мочить голову",
                    Description = "Избегайте попадания воды на область пересадки",
                    Date = firstDayOfMonth.AddDays(5),
                    EventType = EventType.Restriction,
                    IsCompleted = false,
                    ExpirationDate = firstDayOfMonth.AddDays(7) // Add expiration date for restriction
                };
                
                // Медикамент на 10 дней от начала месяца
                var medication = new CalendarEvent
                {
                    Id = 2,
                    Title = "Прием антибиотика",
                    Description = "Азитромицин 500мг, 1 таблетка",
                    Date = firstDayOfMonth.AddDays(10),
                    EventType = EventType.Medication,
                    IsCompleted = false
                };
                
                // Фото на 15 дней от начала месяца
                var photo = new CalendarEvent
                {
                    Id = 3,
                    Title = "Отправить фото",
                    Description = "Сделайте фото области пересадки и отправьте врачу",
                    Date = firstDayOfMonth.AddDays(15),
                    EventType = EventType.Photo,
                    IsCompleted = false
                };
                
                // Инструкция на 20 дней от начала месяца
                var instruction = new CalendarEvent
                {
                    Id = 4,
                    Title = "Начать массаж",
                    Description = "Легкий массаж области пересадки, 5 минут утром и вечером",
                    Date = firstDayOfMonth.AddDays(20),
                    EventType = EventType.Instruction,
                    IsCompleted = false
                };
                
                // Событие на 11 марта 2025
                var march11Event = new CalendarEvent
                {
                    Id = 7,
                    Title = "Контрольный осмотр",
                    Description = "Посещение врача для оценки приживаемости волос",
                    Date = new DateTime(2025, 3, 11),
                    EventType = EventType.Instruction,
                    IsCompleted = false
                };
                
                // Выбранный день (21 марта) имеет несколько событий
                var multipleEvents1 = new CalendarEvent
                {
                    Id = 5,
                    Title = "Прием лекарства",
                    Description = "Миноксидил 5%, нанесение на область пересадки",
                    Date = new DateTime(2025, 3, 21),
                    EventType = EventType.Medication,
                    IsCompleted = false
                };
                
                var multipleEvents2 = new CalendarEvent
                {
                    Id = 6,
                    Title = "Отправить фото",
                    Description = "Сделайте фото области пересадки и отправьте врачу",
                    Date = new DateTime(2025, 3, 21),
                    EventType = EventType.Photo,
                    IsCompleted = false
                };
                
                // Подготовим все события в списке
                var events = new List<CalendarEvent> {
                    restriction, medication, photo, instruction, 
                    march11Event, multipleEvents1, multipleEvents2
                };
                
                // Добавим события в основной поток
                MainThread.BeginInvokeOnMainThread(() => {
                    try 
                    {
                        // Добавляем события в коллекцию
                        foreach (var evt in events) {
                            if (evt != null) {
                                EventsForMonth.Add(evt);
                            }
                        }
                        
                        // Add restriction to active restrictions
                        if (restriction != null && restriction.ExpirationDate.HasValue) {
                            ActiveRestrictions.Add(new RestrictionTimer {
                                RestrictionEvent = restriction
                            });
                        }
                        
                        // Добавляем события для выбранного дня
                        var selectedDate = SelectedDate.Date;
                        var march21 = new DateTime(2025, 3, 21).Date;
                        var march11 = new DateTime(2025, 3, 11).Date;
                        
                        if (selectedDate == march21)
                        {
                            if (multipleEvents1 != null) EventsForSelectedDate.Add(multipleEvents1);
                            if (multipleEvents2 != null) EventsForSelectedDate.Add(multipleEvents2);
                        }
                        else if (selectedDate == march11)
                        {
                            if (march11Event != null) EventsForSelectedDate.Add(march11Event);
                        }
                        
                        // Обновляем календарь после добавления событий
                        UpdateCalendarDays();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding sample events: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating sample events: {ex.Message}");
            }
        }

        private void ExecuteClosePopup()
        {
            IsPopupVisible = false;
        }

        private void ShowPopupForDay(DateTime date)
        {
            SelectedDate = date;
            IsPopupVisible = true;
        }
    }
} 