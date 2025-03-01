using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    // Removed duplicate view model classes - now defined in SharedViewModels.cs

    [INotifyPropertyChanged]
    public partial class CalendarViewModel
    {
        private readonly ICalendarService _calendarService;
        private readonly DayTodoViewModel _dayTodoViewModel;
        private readonly MonthViewModel _monthViewModel;
        private readonly WeekViewModel _weekViewModel;
        private readonly ListViewModel _eventListViewModel;

        [ObservableProperty]
        private int selectedViewIndex;

        [ObservableProperty]
        private string currentPhaseText = string.Empty;

        [ObservableProperty]
        private string progressText = string.Empty;

        [ObservableProperty]
        private double progressValue;

        public string ProgressPercentage => $"{(int)(ProgressValue * 100)}%";

        public MonthViewModel MonthViewModel { get; }
        public WeekViewModel WeekViewModel { get; }
        public ListViewModel ListViewModel { get; }

        public ObservableCollection<MedicationViewModel> TodayMedications { get; } = new();
        public ObservableCollection<RestrictionViewModel> TodayRestrictions { get; } = new();
        public ObservableCollection<InstructionViewModel> TodayInstructions { get; } = new();
        public ObservableCollection<WarningViewModel> TodayWarnings { get; } = new();
        public ObservableCollection<CalendarEventViewModel> UpcomingEvents { get; } = new();
        public ObservableCollection<Syncfusion.Maui.Scheduler.SchedulerAppointment> CalendarAppointments { get; } = new();

        public CalendarViewModel(
            ICalendarService calendarService,
            DayTodoViewModel dayTodoViewModel,
            MonthViewModel monthViewModel,
            WeekViewModel weekViewModel,
            ListViewModel eventListViewModel)
        {
            _calendarService = calendarService;
            _dayTodoViewModel = dayTodoViewModel;
            _monthViewModel = monthViewModel;
            _weekViewModel = weekViewModel;
            _eventListViewModel = eventListViewModel;

            MonthViewModel = monthViewModel;
            WeekViewModel = weekViewModel;
            ListViewModel = eventListViewModel;

            LoadProgressData();
        }

        private void LoadProgressData()
        {
            var currentDay = _calendarService.GetCurrentDay();
            var phase = _calendarService.GetCurrentPhase(currentDay);
            CurrentPhaseText = GetPhaseDisplayName(phase);
            ProgressText = $"День {currentDay} из 180";
            ProgressValue = _calendarService.GetProgressPercentage(currentDay);
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
            LoadData();
            await Task.CompletedTask;
        }

        private void LoadData()
        {
            var currentDay = _calendarService.GetCurrentDay();
            var currentPhase = _calendarService.GetCurrentPhase(currentDay);

            CurrentPhaseText = GetPhaseDisplayName(currentPhase);
            ProgressValue = _calendarService.GetProgressPercentage(currentDay);
            ProgressText = $"День {currentDay} из 180";

            // Initialize with Month view
            SelectedViewIndex = 0;

            // Загружаем данные для сегодняшнего дня
            LoadTodayMedications(currentDay);
            LoadTodayRestrictions(currentDay);
            LoadTodayInstructions(currentDay);
            LoadTodayWarnings(currentDay);
            LoadUpcomingEvents(currentDay);
            LoadCalendarAppointments();
        }

        private void LoadCalendarAppointments()
        {
            CalendarAppointments.Clear();
            var calendarData = _calendarService.GetCalendarData();
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

            var appointment = new Syncfusion.Maui.Scheduler.SchedulerAppointment
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

        // Helper method to set property using reflection
        private void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
        }

        private string GetPhaseDisplayName(RecoveryPhase phase)
        {
            return phase switch
            {
                RecoveryPhase.Initial => "Начальная фаза (0-3 дня)",
                RecoveryPhase.EarlyRecovery => "Раннее восстановление (4-10 дней)",
                RecoveryPhase.Healing => "Фаза заживления (11-30 дней)",
                RecoveryPhase.Growth => "Рост (1-3 месяца)",
                RecoveryPhase.Maturation => "Созревание (4-9 месяца)",
                RecoveryPhase.Final => "Финальная фаза (9-12 месяца)",
                _ => phase.ToString()
            };
        }
    }
} 