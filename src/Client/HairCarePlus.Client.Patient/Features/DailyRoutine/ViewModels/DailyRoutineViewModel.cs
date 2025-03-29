using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.DailyRoutine.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.ViewModels
{
    public partial class DailyRoutineViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private Models.DailyRoutine _todayRoutine;
        private bool _isLoading;
        private bool _isMorningSelected = true;

        [ObservableProperty]
        private DateTime currentDate = DateTime.Now;

        [ObservableProperty]
        private int postOperationDay = 45; // This should be calculated based on the operation date

        [ObservableProperty]
        private int weekDayIndex = 3; // This should be calculated based on the current date

        [ObservableProperty]
        private int currentDay = 6;

        [ObservableProperty]
        private ObservableCollection<DailyTask> dailyTasks;

        public IAsyncRelayCommand LoadDataCommand { get; }

        public DailyRoutineViewModel(
            INavigationService navigationService,
            IVibrationService vibrationService)
        {
            _navigationService = navigationService;
            _vibrationService = vibrationService;
            _todayRoutine = new Models.DailyRoutine
            {
                Date = DateTime.Today,
                MorningRoutines = new List<CareRoutine>(),
                EveningRoutines = new List<CareRoutine>(),
                Medications = new List<Medication>(),
                RequiredProducts = new List<Product>()
            };

            // Commands
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            ToggleRoutineTimeCommand = new Command<bool>(time => IsMorningSelected = time);
            CompleteRoutineCommand = new Command<CareRoutine>(OnCompleteRoutine);
            StartTimerCommand = new Command<CareRoutine>(OnStartTimer);
            WatchVideoGuideCommand = new Command<CareRoutine>(async (routine) => await OnWatchVideoGuide(routine));
            TakeMedicationCommand = new Command<Medication>(OnTakeMedication);
            SkipMedicationCommand = new Command<Medication>(OnSkipMedication);
            BuyProductCommand = new Command<Product>(async (product) => await OnBuyProduct(product));

            LoadTasks();
        }

        public Models.DailyRoutine TodayRoutine
        {
            get => _todayRoutine;
            set => SetProperty(ref _todayRoutine, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsMorningSelected
        {
            get => _isMorningSelected;
            set => SetProperty(ref _isMorningSelected, value);
        }

        public ICommand ToggleRoutineTimeCommand { get; }
        public ICommand CompleteRoutineCommand { get; }
        public ICommand StartTimerCommand { get; }
        public ICommand WatchVideoGuideCommand { get; }
        public ICommand TakeMedicationCommand { get; }
        public ICommand SkipMedicationCommand { get; }
        public ICommand BuyProductCommand { get; }

        private void LoadTasks()
        {
            // Sample data - in real app this would come from a service
            DailyTasks = new ObservableCollection<DailyTask>
            {
                new DailyTask
                {
                    Id = "1",
                    Title = "Take medication",
                    Subtitle = "2 capsules",
                    TaskType = TaskType.Medication,
                    Time = DateTime.Now.Date.AddHours(9),
                    IsCompleted = false
                },
                new DailyTask
                {
                    Id = "2",
                    Title = "Take a photo",
                    TaskType = TaskType.Photo,
                    Time = DateTime.Now,
                    IsCompleted = false
                },
                new DailyTask
                {
                    Id = "3",
                    Title = "Watch massage video",
                    TaskType = TaskType.Video,
                    Time = DateTime.Now,
                    IsCompleted = false
                },
                new DailyTask
                {
                    Id = "4",
                    Title = "Follow-up appointment",
                    Subtitle = "Tomorrow",
                    TaskType = TaskType.Appointment,
                    Time = DateTime.Now.AddDays(1),
                    IsCompleted = false
                },
                new DailyTask
                {
                    Id = "5",
                    Title = "Avoid heavy exercise",
                    TaskType = TaskType.General,
                    Time = DateTime.Now,
                    IsCompleted = false
                }
            };
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                // In a real app, this would load data from a service
                // For now, we'll just use the sample data
                LoadTasks();
                await Task.CompletedTask; // Placeholder for actual async work
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnCompleteRoutine(CareRoutine routine)
        {
            if (routine == null) return;

            routine.IsCompleted = true;
            routine.CompletedAt = DateTime.Now;
            _vibrationService.Vibrate(100); // Короткая вибрация для обратной связи
        }

        private void OnStartTimer(CareRoutine routine)
        {
            if (routine == null) return;
            // TODO: Запуск таймера для процедуры
        }

        private async Task OnWatchVideoGuide(CareRoutine routine)
        {
            if (routine?.VideoGuideUrl == null) return;
            await _navigationService.NavigateToAsync("video/guide", new Dictionary<string, object>
            {
                { "url", routine.VideoGuideUrl }
            });
        }

        private void OnTakeMedication(Medication medication)
        {
            if (medication == null) return;

            medication.IsTaken = true;
            medication.IsSkipped = false;
            medication.TakenAt = DateTime.Now;
            _vibrationService.Vibrate(100);
        }

        private void OnSkipMedication(Medication medication)
        {
            if (medication == null) return;

            medication.IsSkipped = true;
            medication.IsTaken = false;
            _vibrationService.VibrationPattern(new long[] { 0, 100, 100, 100 }); // Двойная вибрация
        }

        private async Task OnBuyProduct(Product product)
        {
            if (product?.PurchaseUrl == null) return;
            await _navigationService.NavigateToAsync("product/purchase", new Dictionary<string, object>
            {
                { "url", product.PurchaseUrl }
            });
        }
    }
} 