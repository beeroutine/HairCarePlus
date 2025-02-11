using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Models;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Services;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public class TreatmentProgressViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private readonly TreatmentCalendarService _calendarService;
        private ObservableCollection<DayViewModel> _weekDays;
        private ObservableCollection<TreatmentEvent> _dailyEvents;
        private DateTime _selectedDate;
        private int _completedTasks;
        private double _dailyProgress;
        private bool _isLoading;
        private List<TreatmentEvent> _allEvents;

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

            Title = "My Progress";

            // Commands
            SelectDateCommand = new Command<DayViewModel>(OnDateSelected);
            CompleteEventCommand = new Command<TreatmentEvent>(OnEventCompleted);

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
            set => SetProperty(ref _dailyProgress, value);
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
            _allEvents = _calendarService.GenerateCalendar(surgeryDate);

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
                var dateEvents = _allEvents.Where(e => e.Date.Date == date.Date).ToList();
                
                WeekDays.Add(new DayViewModel
                {
                    Date = date.Day.ToString(),
                    DayOfWeek = date.ToString("ddd"),
                    FullDate = date,
                    IsSelected = date.Date == SelectedDate.Date,
                    HasTasks = dateEvents.Any(),
                    HasEvents = dateEvents.Any(),
                    TaskCount = dateEvents.Count,
                    TaskTypes = string.Join(", ", dateEvents.Select(e => e.Type).Distinct())
                });
            }
        }

        private void UpdateDailyEvents()
        {
            DailyEvents.Clear();
            var events = _allEvents.Where(e => e.Date.Date == SelectedDate.Date).ToList();
            foreach (var evt in events)
            {
                DailyEvents.Add(evt);
            }

            UpdateDailyProgress();
        }

        private void OnDateSelected(DayViewModel day)
        {
            if (day == null) return;

            foreach (var d in WeekDays)
            {
                d.IsSelected = d.FullDate.Date == day.FullDate.Date;
            }

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

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
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
            });
        }
    }
} 