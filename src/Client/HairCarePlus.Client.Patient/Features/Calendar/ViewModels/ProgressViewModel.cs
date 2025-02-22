using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Data;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class PhaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty]
        private bool isCompleted;

        [ObservableProperty]
        private string status;
    }

    public partial class ExpectedChangeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;
    }

    public partial class ProgressViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isActive;

        [ObservableProperty]
        private string name;

        private readonly IPostOperationCalendarService _calendarService;

        [ObservableProperty]
        private string currentPhaseText;

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressText;

        public ObservableCollection<PhaseViewModel> Phases { get; } = new();
        public ObservableCollection<KeyValuePair<string, string>> ExpectedChanges { get; } = new();
        public ObservableCollection<MilestoneViewModel> Milestones { get; } = new();

        public ProgressViewModel(IPostOperationCalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadData();
        }

        private void LoadData()
        {
            var currentDay = _calendarService.GetCurrentDay();
            var currentPhase = _calendarService.GetCurrentPhase();

            // Update progress info
            CurrentPhaseText = GetPhaseDisplayName(currentPhase);
            ProgressPercentage = _calendarService.GetProgressPercentage() / 100.0;
            ProgressText = $"День {currentDay} из 365";

            // Load phases
            LoadPhases(currentPhase);

            // Load expected changes
            LoadExpectedChanges(currentPhase);

            // Load milestones
            LoadMilestones(currentDay);
        }

        private void LoadPhases(RecoveryPhase currentPhase)
        {
            Phases.Clear();
            foreach (RecoveryPhase phase in Enum.GetValues(typeof(RecoveryPhase)))
            {
                var isCompleted = _calendarService.IsPhaseCompleted(phase);
                var isActive = phase == currentPhase;

                Phases.Add(new PhaseViewModel
                {
                    Name = GetPhaseDisplayName(phase),
                    Description = GetPhaseDescription(phase),
                    IsActive = isActive,
                    IsCompleted = isCompleted,
                    Status = GetPhaseStatus(isCompleted, isActive)
                });
            }
        }

        private void LoadExpectedChanges(RecoveryPhase currentPhase)
        {
            ExpectedChanges.Clear();
            var conditions = _calendarService.GetExpectedConditionsForPhase(currentPhase);
            foreach (var condition in conditions)
            {
                ExpectedChanges.Add(condition);
            }
        }

        private void LoadMilestones(int currentDay)
        {
            Milestones.Clear();
            var milestoneEvents = PostOperationCalendarData.Events
                .OfType<MilestoneEvent>()
                .OrderBy(m => m.StartDay);

            foreach (var milestone in milestoneEvents)
            {
                Milestones.Add(new MilestoneViewModel
                {
                    Name = milestone.Name,
                    Description = milestone.Description,
                    DayNumber = milestone.StartDay,
                    IsCompleted = currentDay >= milestone.StartDay,
                    UnlockedActivities = milestone.UnlockedActivities.ToList()
                });
            }
        }

        private string GetPhaseDisplayName(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Начальная фаза (0-3 дня)",
                RecoveryPhase.EarlyRecovery => "Раннее восстановление (4-10 дней)",
                RecoveryPhase.Healing => "Заживление (11-30 дней)",
                RecoveryPhase.Growth => "Рост (1-3 месяца)",
                RecoveryPhase.Development => "Развитие (4-8 месяца)",
                RecoveryPhase.Final => "Финальная фаза (9-12 месяца)",
                _ => phase.ToString()
            };
        }

        private string GetPhaseDescription(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Период непосредственно после операции",
                RecoveryPhase.EarlyRecovery => "Формирование корочек и начало заживления",
                RecoveryPhase.Healing => "Полное заживление донорской зоны",
                RecoveryPhase.Growth => "Начало роста новых волос",
                RecoveryPhase.Development => "Активный рост и укрепление волос",
                RecoveryPhase.Final => "Формирование окончательного результата",
                _ => string.Empty
            };
        }

        private string GetPhaseStatus(bool isCompleted, bool isActive)
        {
            if (isActive) return "Текущая";
            return isCompleted ? "Завершена" : "Предстоит";
        }
    }

    public partial class MilestoneViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private int dayNumber;

        [ObservableProperty]
        private bool isCompleted;

        [ObservableProperty]
        private List<string> unlockedActivities;
    }
} 