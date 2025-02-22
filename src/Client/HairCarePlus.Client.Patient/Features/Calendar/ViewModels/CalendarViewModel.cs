using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Views;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class MedicationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string instructions;

        [ObservableProperty]
        private string dosage;

        [ObservableProperty]
        private int timesPerDay;

        [ObservableProperty]
        private bool isOptional;
    }

    public partial class RestrictionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string reason;

        [ObservableProperty]
        private bool isCritical;

        [ObservableProperty]
        private string recommendedAlternative;
    }

    public partial class InstructionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string[] steps;
    }

    public partial class WarningViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;
    }

    public partial class CalendarViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        private readonly IPostOperationCalendarService _calendarService;

        [ObservableProperty]
        private string currentPhaseText;

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressText;

        [ObservableProperty]
        private bool isRefreshing;

        public ObservableCollection<MedicationViewModel> TodayMedications { get; } = new();
        public ObservableCollection<RestrictionViewModel> TodayRestrictions { get; } = new();
        public ObservableCollection<InstructionViewModel> TodayInstructions { get; } = new();
        public ObservableCollection<WarningViewModel> TodayWarnings { get; } = new();
        public ObservableCollection<CalendarEventViewModel> UpcomingEvents { get; } = new();

        public CalendarViewModel(IPostOperationCalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadData();
        }

        [RelayCommand]
        private async Task ShowDayDetails()
        {
            var currentDay = _calendarService.GetCurrentDay();
            var parameters = new Dictionary<string, object>
            {
                { "dayNumber", currentDay }
            };
            await Shell.Current.GoToAsync(nameof(DayDetailsPage), parameters);
        }

        [RelayCommand]
        private async Task ShowProgress()
        {
            await Shell.Current.GoToAsync(nameof(ProgressPage));
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            try
            {
                LoadData();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void LoadData()
        {
            var currentDay = _calendarService.GetCurrentDay();
            var currentPhase = _calendarService.GetCurrentPhase();

            // Update phase and progress
            CurrentPhaseText = $"Фаза: {GetPhaseDisplayName(currentPhase)}";
            ProgressPercentage = _calendarService.GetProgressPercentage() / 100.0;
            ProgressText = $"День {currentDay} из 365";

            // Load today's events
            LoadTodayMedications(currentDay);
            LoadTodayRestrictions(currentDay);
            LoadTodayInstructions(currentDay);
            LoadTodayWarnings(currentDay);
            LoadUpcomingEvents(currentDay);
        }

        private void LoadTodayMedications(int currentDay)
        {
            TodayMedications.Clear();
            foreach (var med in _calendarService.GetMedicationsForDay(currentDay))
            {
                TodayMedications.Add(new MedicationViewModel
                {
                    Name = med.MedicationName,
                    Description = med.Description,
                    Instructions = med.Instructions,
                    Dosage = med.Dosage,
                    TimesPerDay = med.TimesPerDay,
                    IsOptional = med.IsOptional
                });
            }
        }

        private void LoadTodayRestrictions(int currentDay)
        {
            TodayRestrictions.Clear();
            foreach (var restriction in _calendarService.GetActiveRestrictionsForDay(currentDay))
            {
                TodayRestrictions.Add(new RestrictionViewModel
                {
                    Name = restriction.Name,
                    Description = restriction.Description,
                    Reason = restriction.Reason,
                    IsCritical = restriction.IsCritical,
                    RecommendedAlternative = restriction.RecommendedAlternative
                });
            }
        }

        private void LoadTodayInstructions(int currentDay)
        {
            TodayInstructions.Clear();
            var washingInstructions = _calendarService.GetWashingInstructionsForDay(currentDay);
            if (washingInstructions != null)
            {
                var instructionEvent = new InstructionViewModel
                {
                    Name = washingInstructions.Name,
                    Description = washingInstructions.Description,
                    Steps = washingInstructions.Steps.Select(s => s.Description).ToArray()
                };
                TodayInstructions.Add(instructionEvent);
            }

            var instructions = _calendarService.GetInstructionsForDay(currentDay);
            if (instructions != null)
            {
                TodayInstructions.Add(new InstructionViewModel
                {
                    Name = instructions.Name,
                    Description = instructions.Description,
                    Steps = instructions.Steps
                });
            }
        }

        private void LoadTodayWarnings(int currentDay)
        {
            TodayWarnings.Clear();
            foreach (var warning in _calendarService.GetWarningsForDay(currentDay))
            {
                TodayWarnings.Add(new WarningViewModel
                {
                    Name = warning.Name,
                    Description = warning.Description
                });
            }
        }

        private void LoadUpcomingEvents(int currentDay)
        {
            UpcomingEvents.Clear();
            var nextWeekEvents = new List<CalendarEventViewModel>();

            // Look ahead for the next 7 days
            for (int day = currentDay + 1; day <= currentDay + 7; day++)
            {
                var events = _calendarService.GetEventsForDay(day)
                    .Select(e => new CalendarEventViewModel
                    {
                        DayNumber = day,
                        Name = e.Name,
                        Description = e.Description
                    });

                nextWeekEvents.AddRange(events);
            }

            foreach (var ev in nextWeekEvents.OrderBy(e => e.DayNumber))
            {
                UpcomingEvents.Add(ev);
            }
        }

        private string GetPhaseDisplayName(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Начальная (0-3 дня)",
                RecoveryPhase.EarlyRecovery => "Раннее восстановление (4-10 дней)",
                RecoveryPhase.Healing => "Заживление (11-30 дней)",
                RecoveryPhase.Growth => "Рост (1-3 месяца)",
                RecoveryPhase.Development => "Развитие (4-8 месяца)",
                RecoveryPhase.Final => "Финальная (9-12 месяца)",
                _ => phase.ToString()
            };
        }
    }

    public partial class CalendarEventViewModel : ObservableObject
    {
        [ObservableProperty]
        private int dayNumber;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;
    }
} 