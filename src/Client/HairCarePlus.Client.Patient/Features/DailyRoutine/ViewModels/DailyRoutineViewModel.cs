using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.DailyRoutine.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.DailyRoutine.ViewModels
{
    public class DailyRoutineViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private Models.DailyRoutine _todayRoutine;
        private bool _isLoading;
        private bool _isMorningSelected = true;

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

            Title = "Daily Care";

            // Команды
            ToggleRoutineTimeCommand = new Command<bool>(time => IsMorningSelected = time);
            CompleteRoutineCommand = new Command<CareRoutine>(OnCompleteRoutine);
            StartTimerCommand = new Command<CareRoutine>(OnStartTimer);
            WatchVideoGuideCommand = new Command<CareRoutine>(async (routine) => await OnWatchVideoGuide(routine));
            TakeMedicationCommand = new Command<Medication>(OnTakeMedication);
            SkipMedicationCommand = new Command<Medication>(OnSkipMedication);
            BuyProductCommand = new Command<Product>(async (product) => await OnBuyProduct(product));
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

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    // TODO: Загрузка данных с сервера
                    await Task.Delay(1000); // Имитация загрузки
                    TodayRoutine = new Models.DailyRoutine
                    {
                        Date = DateTime.Today,
                        MorningRoutines = new List<CareRoutine>
                        {
                            new CareRoutine
                            {
                                Title = "Gentle Scalp Massage",
                                Description = "Stimulate blood flow with gentle massage",
                                Duration = TimeSpan.FromMinutes(5),
                                Steps = new List<string>
                                {
                                    "Use fingertips, not nails",
                                    "Apply gentle pressure",
                                    "Massage in circular motions",
                                    "Focus on transplanted areas"
                                },
                                Priority = Priority.High,
                                VideoGuideUrl = "https://example.com/massage-guide"
                            }
                        },
                        EveningRoutines = new List<CareRoutine>
                        {
                            new CareRoutine
                            {
                                Title = "Apply Special Serum",
                                Description = "Use the prescribed serum for better growth",
                                Duration = TimeSpan.FromMinutes(2),
                                Steps = new List<string>
                                {
                                    "Clean your hands",
                                    "Apply 2-3 drops to each area",
                                    "Gently pat, don't rub",
                                    "Let it absorb for 5 minutes"
                                },
                                Priority = Priority.Critical,
                                VideoGuideUrl = "https://example.com/serum-guide"
                            }
                        },
                        Medications = new List<Medication>
                        {
                            new Medication
                            {
                                Name = "Growth Support Complex",
                                Dosage = "1 tablet",
                                Time = new TimeSpan(8, 0, 0),
                                Instructions = "Take with water after breakfast"
                            }
                        },
                        RequiredProducts = new List<Product>
                        {
                            new Product
                            {
                                Name = "Special Care Serum",
                                Description = "Essential for hair growth",
                                RemainingDays = 5,
                                PurchaseUrl = "https://example.com/serum"
                            }
                        }
                    };
                }
                finally
                {
                    IsLoading = false;
                }
            });
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