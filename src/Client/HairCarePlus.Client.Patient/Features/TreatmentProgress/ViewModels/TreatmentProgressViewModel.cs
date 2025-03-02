using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Models;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Services;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public partial class TreatmentProgressViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private readonly TreatmentCalendarService _calendarService;
        private ObservableCollection<DayViewModel> _weekDays;
        private ObservableCollection<TreatmentEvent> _dailyEvents;
        private DateTime _selectedDate;
        private int _completedTasks;
        private double _dailyProgress;
        private double _dailyProgressWidth;
        private bool _isLoading;
        private List<TreatmentEvent> _allEvents;

        [ObservableProperty]
        private string title = "Мой прогресс";

        public IAsyncRelayCommand OpenChatCommand { get; }

        public TreatmentProgressViewModel(
            INavigationService navigationService,
            IVibrationService vibrationService)
        {
            _navigationService = navigationService;
            _vibrationService = vibrationService;
            _calendarService = new TreatmentCalendarService();
            _weekDays = new ObservableCollection<DayViewModel>();
            _dailyEvents = new ObservableCollection<TreatmentEvent>();
            _selectedDate = DateTime.Today;
            _allEvents = new List<TreatmentEvent>();

            // Commands
            SelectDateCommand = new Command<DayViewModel>(OnDateSelected);
            CompleteEventCommand = new Command<TreatmentEvent>(OnEventCompleted);
            OpenChatCommand = new AsyncRelayCommand(OpenChatAsync);

            InitializeCalendar();
        }

        public ObservableCollection<DayViewModel> WeekDays
        {
            get => _weekDays;
            set => SetProperty(ref _weekDays, value);
        }

        public ObservableCollection<TreatmentEvent> DailyEvents
        {
            get => _dailyEvents;
            set => SetProperty(ref _dailyEvents, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    UpdateDailyEvents();
                }
            }
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set
            {
                if (SetProperty(ref _completedTasks, value))
                {
                    UpdateDailyProgress();
                }
            }
        }

        public double DailyProgress
        {
            get => _dailyProgress;
            set
            {
                if (SetProperty(ref _dailyProgress, value))
                {
                    // Update the progress bar width when progress changes
                    DailyProgressWidth = _dailyProgress * 100;
                }
            }
        }
        
        public double DailyProgressWidth
        {
            get => _dailyProgressWidth;
            set => SetProperty(ref _dailyProgressWidth, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SelectDateCommand { get; }
        public ICommand CompleteEventCommand { get; }

        private void InitializeCalendar()
        {
            // TODO: В реальном приложении дата операции должна приходить с сервера
            var surgeryDate = new DateTime(2025, 1, 15); // Пример даты операции
            
            // Use lazy loading instead of generating all events upfront
            _allEvents = new List<TreatmentEvent>();
            
            // Just get events for today initially
            var todayEvents = _calendarService.GetEventsForDay(surgeryDate, DateTime.Today);
            _allEvents.AddRange(todayEvents);

            // Make sure today is selected by default
            _selectedDate = DateTime.Today;

            UpdateCalendarDays();
            UpdateDailyEvents();
        }

        private void UpdateCalendarDays()
        {
            var today = DateTime.Today;
            WeekDays.Clear();

            // Показываем 14 дней вперед
            for (int i = 0; i <= 14; i++)
            {
                var date = today.AddDays(i);
                
                // Only check if we have events for this day, don't actually load them all
                bool hasEventsForDay = _calendarService.HasEventsForDay(date);
                int eventCount = hasEventsForDay ? _calendarService.GetEventCountForDay(date) : 0;
                
                var dayVm = new DayViewModel
                {
                    Date = date.Day.ToString(),
                    DayOfWeek = date.ToString("ddd"),
                    FullDate = date,
                    IsSelected = date.Date == SelectedDate.Date,
                    HasTasks = hasEventsForDay,
                    HasEvents = hasEventsForDay,
                    TaskCount = eventCount,
                    IsToday = date.Date == DateTime.Today
                };
                
                // Get first event type for icon (if any)
                if (hasEventsForDay)
                {
                    // Get the event type without loading all events
                    var eventType = _calendarService.GetFirstEventTypeForDay(date);
                    dayVm.TaskTypes = eventType;
                }
                
                WeekDays.Add(dayVm);
            }
        }

        private void UpdateDailyEvents()
        {
            DailyEvents.Clear();
            
            // Only load events for the selected day on demand
            if (!_allEvents.Any(e => e.Date.Date == SelectedDate.Date))
            {
                // TODO: In a real app, we'd load this from the server
                var surgeryDate = new DateTime(2025, 1, 15);
                var events = _calendarService.GetEventsForDay(surgeryDate, SelectedDate);
                _allEvents.AddRange(events);
            }
            
            var selectedDayEvents = _allEvents.Where(e => e.Date.Date == SelectedDate.Date).ToList();
            foreach (var evt in selectedDayEvents)
            {
                DailyEvents.Add(evt);
            }

            UpdateDailyProgress();
        }

        private void OnDateSelected(DayViewModel day)
        {
            if (day == null) return;

            // Deselect previously selected day
            foreach (var d in WeekDays)
            {
                d.IsSelected = false;
            }

            // Select new day
            day.IsSelected = true;
            SelectedDate = day.FullDate;
            _vibrationService.Vibrate(50);
        }

        private void OnEventCompleted(TreatmentEvent evt)
        {
            if (evt == null) return;

            evt.IsCompleted = true;
            CompletedTasks++;
            _vibrationService.VibrationPattern(new long[] { 0, 100, 50, 100 });

            // Обновляем прогресс
            UpdateDailyProgress();
        }

        private void UpdateDailyProgress()
        {
            var totalEvents = DailyEvents.Count;
            var completedEvents = DailyEvents.Count(e => e.IsCompleted);
            DailyProgress = totalEvents > 0 ? (double)completedEvents / totalEvents : 0;
        }

        private async Task OpenChatAsync()
        {
            await Shell.Current.GoToAsync("//chat");
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // В реальном приложении здесь будет загрузка с сервера
                await Task.Delay(500);
                InitializeCalendar();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
} 