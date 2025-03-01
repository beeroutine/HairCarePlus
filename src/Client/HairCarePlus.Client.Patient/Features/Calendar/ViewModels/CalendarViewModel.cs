using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Views;
using HairCarePlus.Client.Patient.Features.Calendar.Data;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Syncfusion.Maui.Scheduler;
using Microsoft.Maui.Graphics;
using System.Reflection;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    // Removed duplicate view model classes - now defined in SharedViewModels.cs

    public partial class CalendarViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        private readonly ICalendarService _calendarService;

        [ObservableProperty]
        private string currentPhaseText;

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressText;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private int selectedViewIndex;

        [ObservableProperty]
        private SchedulerView selectedView = SchedulerView.Month;

        public ObservableCollection<MedicationViewModel> TodayMedications { get; } = new();
        public ObservableCollection<RestrictionViewModel> TodayRestrictions { get; } = new();
        public ObservableCollection<InstructionViewModel> TodayInstructions { get; } = new();
        public ObservableCollection<WarningViewModel> TodayWarnings { get; } = new();
        public ObservableCollection<CalendarEventViewModel> UpcomingEvents { get; } = new();
        public ObservableCollection<SchedulerAppointment> CalendarAppointments { get; } = new();

        public CalendarViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            LoadData();

            // Наблюдаем за изменением индекса представления
            this.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(SelectedViewIndex))
                {
                    SelectedView = SelectedViewIndex switch
                    {
                        0 => SchedulerView.Month,
                        1 => SchedulerView.Week,
                        2 => SchedulerView.Day,
                        3 => SchedulerView.Agenda,
                        _ => SchedulerView.Month
                    };
                }
            };
        }

        [RelayCommand]
        private async Task ShowDayDetails()
        {
            var navigationService = ServiceHelper.GetService<INavigationService>();
            if (navigationService != null)
            {
                await navigationService.NavigateToAsync<DayDetailsViewModel>(new Dictionary<string, object>
                {
                    { "Date", DateTime.Today }
                });
            }
        }

        [RelayCommand]
        private async Task ShowProgress()
        {
            var navigationService = ServiceHelper.GetService<INavigationService>();
            if (navigationService != null)
            {
                await navigationService.NavigateToAsync<ProgressViewModel>();
            }
        }

        [RelayCommand]
        private async Task AddEvent()
        {
            // Здесь будет логика добавления нового события
            await Task.CompletedTask;
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
            await Task.CompletedTask;
        }

        private void LoadData()
        {
            var calendarData = _calendarService.GetCalendarData();
            var currentDay = (DateTime.Now - calendarData.OperationDate).Days + 1;
            var totalDays = 180; // Примерно 6 месяцев на восстановление

            // Обновляем информацию о прогрессе
            ProgressPercentage = Math.Min(1.0, Math.Max(0, (double)currentDay / totalDays));
            ProgressText = $"День {currentDay} из {totalDays}";

            // Определяем текущую фазу
            var phase = _calendarService.GetCurrentPhase(currentDay);
            CurrentPhaseText = GetPhaseDisplayName(phase);

            // Загружаем данные для сегодняшнего дня
            LoadTodayMedications(currentDay);
            LoadTodayRestrictions(currentDay);
            LoadTodayInstructions(currentDay);
            LoadTodayWarnings(currentDay);
            LoadUpcomingEvents(currentDay);
            LoadCalendarAppointments(calendarData);
        }

        private void LoadCalendarAppointments(CalendarDataModel calendarData)
        {
            CalendarAppointments.Clear();
            var operationDate = calendarData.OperationDate;

            // Словарь цветов для разных типов событий
            var eventColors = new Dictionary<EventType, (Color Background, Color TextColor, string Icon)>
            {
                { EventType.Medication, (Color.FromArgb("#4CAF50"), Colors.White, "\uf484") },
                { EventType.PhotoUpload, (Color.FromArgb("#2196F3"), Colors.White, "\uf030") },
                { EventType.Instruction, (Color.FromArgb("#9C27B0"), Colors.White, "\uf05a") },
                { EventType.Milestone, (Color.FromArgb("#FF9800"), Colors.White, "\uf091") },
                { EventType.PRP, (Color.FromArgb("#F44336"), Colors.White, "\uf0fa") },
                { EventType.Restriction, (Color.FromArgb("#607D8B"), Colors.White, "\uf05e") },
                { EventType.Warning, (Color.FromArgb("#FF5722"), Colors.White, "\uf071") },
                { EventType.WashingInstruction, (Color.FromArgb("#00BCD4"), Colors.White, "\uf043") },
                { EventType.ProgressCheck, (Color.FromArgb("#8BC34A"), Colors.White, "\uf201") }
            };

            // Добавляем все события в календарь
            foreach (var evt in calendarData.Events)
            {
                var startDate = operationDate.AddDays(evt.StartDay - 1);
                var endDate = evt.EndDay.HasValue 
                    ? operationDate.AddDays(evt.EndDay.Value - 1) 
                    : startDate;
                
                // Для повторяющихся событий
                if (evt.IsRepeating && evt.RepeatIntervalDays.HasValue)
                {
                    var currentDate = startDate;
                    var endRepeatDate = operationDate.AddDays(180); // 6 месяцев
                    
                    while (currentDate <= endRepeatDate)
                    {
                        AddAppointment(evt, currentDate, currentDate, eventColors);
                        currentDate = currentDate.AddDays(evt.RepeatIntervalDays.Value);
                    }
                }
                else
                {
                    // Для обычных событий
                    AddAppointment(evt, startDate, endDate, eventColors);
                }
            }
        }

        private void AddAppointment(
            CalendarEvent evt, 
            DateTime startDate, 
            DateTime endDate, 
            Dictionary<EventType, (Color Background, Color TextColor, string Icon)> eventColors)
        {
            var (background, textColor, icon) = eventColors.ContainsKey(evt.Type) 
                ? eventColors[evt.Type] 
                : (Color.FromArgb("#9E9E9E"), Colors.White, "\uf128");

            var appointment = new SchedulerAppointment
            {
                Subject = evt.Name,
                Notes = evt.Description,
                StartTime = startDate,
                EndTime = endDate.AddDays(1).AddSeconds(-1), // До конца дня
                Background = background,
                TextColor = textColor
            };

            // Note: Since SchedulerAppointment doesn't have a Data property in this version,
            // we'll skip setting it for now

            CalendarAppointments.Add(appointment);
        }

        private async void LoadTodayMedications(int currentDay)
        {
            TodayMedications.Clear();
            var medications = await _calendarService.GetMedicationsForDayAsync(currentDay);

            foreach (var med in medications)
            {
                // Create a new instance and set properties using reflection
                var viewModel = new MedicationViewModel();
                SetProperty(viewModel, "Name", med.Name);
                SetProperty(viewModel, "Description", med.Description);
                SetProperty(viewModel, "Instructions", med.Instructions);
                SetProperty(viewModel, "Dosage", med.Dosage);
                SetProperty(viewModel, "TimesPerDay", med.TimesPerDay);
                SetProperty(viewModel, "IsOptional", med.IsOptional);
                TodayMedications.Add(viewModel);
            }
        }

        private async void LoadTodayRestrictions(int currentDay)
        {
            TodayRestrictions.Clear();
            var restrictions = await _calendarService.GetRestrictionsForDayAsync(currentDay);

            foreach (var restriction in restrictions)
            {
                var viewModel = new RestrictionViewModel();
                SetProperty(viewModel, "Name", restriction.Name);
                SetProperty(viewModel, "Description", restriction.Description);
                SetProperty(viewModel, "Reason", restriction.Reason);
                SetProperty(viewModel, "IsCritical", restriction.IsCritical);
                SetProperty(viewModel, "RecommendedAlternative", restriction.RecommendedAlternative);
                TodayRestrictions.Add(viewModel);
            }
        }

        private async void LoadTodayInstructions(int currentDay)
        {
            TodayInstructions.Clear();
            var instructions = await _calendarService.GetInstructionsForDayAsync(currentDay);

            foreach (var instruction in instructions)
            {
                var viewModel = new InstructionViewModel();
                SetProperty(viewModel, "Name", instruction.Name);
                SetProperty(viewModel, "Description", instruction.Description);
                SetProperty(viewModel, "Steps", instruction.Steps);
                TodayInstructions.Add(viewModel);
            }
        }

        private async void LoadTodayWarnings(int currentDay)
        {
            TodayWarnings.Clear();
            var warnings = await _calendarService.GetWarningsForDayAsync(currentDay);

            foreach (var warning in warnings)
            {
                var viewModel = new WarningViewModel();
                SetProperty(viewModel, "Name", warning.Name);
                SetProperty(viewModel, "Description", warning.Description);
                TodayWarnings.Add(viewModel);
            }
        }

        private async void LoadUpcomingEvents(int currentDay)
        {
            UpcomingEvents.Clear();
            var upcomingEvents = await _calendarService.GetEventsForDayAsync(currentDay + 7);

            foreach (var evt in upcomingEvents)
            {
                var viewModel = new CalendarEventViewModel();
                SetProperty(viewModel, "Name", evt.Name);
                SetProperty(viewModel, "Description", evt.Description);
                SetProperty(viewModel, "DayNumber", evt.StartDay);
                UpcomingEvents.Add(viewModel);
            }
        }

        private string GetPhaseDisplayName(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Начальная фаза (0-3 дня)",
                RecoveryPhase.EarlyRecovery => "Ранняя фаза (4-10 дней)",
                RecoveryPhase.Healing => "Фаза заживления (11-30 дней)",
                RecoveryPhase.Growth => "Фаза роста (1-3 месяца)",
                RecoveryPhase.Maturation => "Фаза созревания (4-9 месяцев)",
                RecoveryPhase.Final => "Финальная фаза (9-12 месяцев)",
                _ => "Неизвестная фаза"
            };
        }

        // Helper method to set property using reflection
        private void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(obj, value);
            }
        }
    }
} 