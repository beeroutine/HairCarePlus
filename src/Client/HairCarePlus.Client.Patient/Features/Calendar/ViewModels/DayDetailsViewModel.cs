using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public partial class DayDetailsViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        private readonly IPostOperationCalendarService _calendarService;
        private int _dayNumber;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string dayText;

        [ObservableProperty]
        private string dateText;

        [ObservableProperty]
        private string phaseText;

        public ObservableCollection<MedicationViewModel> Medications { get; } = new();
        public ObservableCollection<RestrictionViewModel> Restrictions { get; } = new();
        public ObservableCollection<InstructionViewModel> Instructions { get; } = new();
        public ObservableCollection<WarningViewModel> Warnings { get; } = new();

        public DayDetailsViewModel(IPostOperationCalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("dayNumber", out var dayNumber))
            {
                _dayNumber = Convert.ToInt32(dayNumber);
                LoadData();
            }
        }

        private void LoadData()
        {
            var phase = _calendarService.GetCurrentPhase();
            DayText = $"День {_dayNumber}";
            DateText = DateTime.Now.AddDays(_dayNumber - _calendarService.GetCurrentDay()).ToString("d MMMM yyyy");
            PhaseText = GetPhaseDisplayName(phase);
            Title = $"День {_dayNumber} - {GetPhaseDisplayName(phase)}";

            LoadMedications();
            LoadRestrictions();
            LoadInstructions();
            LoadWarnings();
        }

        private void LoadMedications()
        {
            Medications.Clear();
            foreach (var med in _calendarService.GetMedicationsForDay(_dayNumber))
            {
                Medications.Add(new MedicationViewModel
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

        private string GetPhaseDisplayName(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Начальная фаза",
                RecoveryPhase.EarlyRecovery => "Ранняя реабилитация",
                RecoveryPhase.Healing => "Заживление",
                RecoveryPhase.Growth => "Рост",
                RecoveryPhase.Development => "Развитие",
                RecoveryPhase.Final => "Финальная фаза",
                _ => phase.ToString()
            };
        }

        [RelayCommand]
        private Task PreviousDay()
        {
            if (_dayNumber > 0)
            {
                _dayNumber--;
                LoadData();
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task NextDay()
        {
            if (_dayNumber < 365)
            {
                _dayNumber++;
                LoadData();
            }
            return Task.CompletedTask;
        }

        private void LoadRestrictions()
        {
            Restrictions.Clear();
            foreach (var restriction in _calendarService.GetActiveRestrictionsForDay(_dayNumber))
            {
                Restrictions.Add(new RestrictionViewModel
                {
                    Name = restriction.Name,
                    Description = restriction.Description,
                    Reason = restriction.Reason,
                    IsCritical = restriction.IsCritical
                });
            }
        }

        private void LoadInstructions()
        {
            Instructions.Clear();
            var washingInstructions = _calendarService.GetWashingInstructionsForDay(_dayNumber);
            if (washingInstructions != null)
            {
                var instructionEvent = new InstructionViewModel
                {
                    Name = washingInstructions.Name,
                    Description = washingInstructions.Description,
                    Steps = washingInstructions.Steps.Select(s => s.Description).ToArray()
                };
                Instructions.Add(instructionEvent);
            }

            var instructions = _calendarService.GetInstructionsForDay(_dayNumber);
            if (instructions != null)
            {
                Instructions.Add(new InstructionViewModel
                {
                    Name = instructions.Name,
                    Description = instructions.Description,
                    Steps = instructions.Steps
                });
            }
        }

        private void LoadWarnings()
        {
            Warnings.Clear();
            foreach (var warning in _calendarService.GetWarningsForDay(_dayNumber))
            {
                Warnings.Add(new WarningViewModel
                {
                    Name = warning.Name,
                    Description = warning.Description
                });
            }
        }
    }
} 