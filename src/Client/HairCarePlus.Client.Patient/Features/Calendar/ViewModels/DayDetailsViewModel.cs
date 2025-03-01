using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class DayMedicationViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Instructions { get; set; }
        public string Dosage { get; set; }
        public int TimesPerDay { get; set; }
        public bool IsOptional { get; set; }
    }
    
    public class DayRestrictionViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public bool IsCritical { get; set; }
        public string RecommendedAlternative { get; set; }
    }
    
    public class DayInstructionViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Steps { get; set; }
    }
    
    public class DayWarningViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public partial class DayDetailsViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ICalendarService _calendarService;
        
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }
        
        private int _dayNumber;
        public int DayNumber
        {
            get => _dayNumber;
            set => SetProperty(ref _dayNumber, value);
        }
        
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        
        private string _dateText;
        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }
        
        private string _dayText;
        public string DayText
        {
            get => _dayText;
            set => SetProperty(ref _dayText, value);
        }
        
        public ObservableCollection<DayMedicationViewModel> Medications { get; } = new();
        public ObservableCollection<DayRestrictionViewModel> Restrictions { get; } = new();
        public ObservableCollection<DayInstructionViewModel> Instructions { get; } = new();
        public ObservableCollection<DayWarningViewModel> Warnings { get; } = new();
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        public ICommand LoadDataCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        
        public DayDetailsViewModel(ICalendarService calendarService)
        {
            _calendarService = calendarService;
            _date = DateTime.Today;
            
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
        }
        
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("date", out var dateObj) && dateObj is string dateStr)
            {
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    Date = parsedDate;
                    ((AsyncRelayCommand)LoadDataCommand).Execute(null);
                }
            }
        }
        
        private void PreviousDay()
        {
            Date = Date.AddDays(-1);
            ((AsyncRelayCommand)LoadDataCommand).Execute(null);
        }
        
        private void NextDay()
        {
            Date = Date.AddDays(1);
            ((AsyncRelayCommand)LoadDataCommand).Execute(null);
        }
        
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            
            try
            {
                // Обновляем заголовок и информацию о дате
                DateText = Date.ToString("d MMMM yyyy");
                DayText = Date.ToString("dddd");
                
                // Вычисляем номер дня относительно даты операции
                DateTime operationDate = _calendarService.GetOperationDate();
                DayNumber = (Date - operationDate).Days + 1;
                Title = DayNumber == 1 ? "День операции" : $"День {DayNumber}";
                
                // Загружаем данные для выбранного дня
                await LoadMedicationsAsync();
                await LoadRestrictionsAsync();
                await LoadInstructionsAsync();
                await LoadWarningsAsync();
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь должна быть обработка ошибок
                System.Diagnostics.Debug.WriteLine($"Error loading day details: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task LoadMedicationsAsync()
        {
            Medications.Clear();
            
            try
            {
                // Получаем медикаменты для выбранного дня
                var medications = await _calendarService.GetMedicationsForDayAsync(DayNumber);
                
                foreach (var med in medications)
                {
                    var vm = new DayMedicationViewModel();
                    vm.Name = med.Name;
                    vm.Description = med.Description;
                    vm.Instructions = med.Instructions;
                    vm.Dosage = med.Dosage;
                    vm.TimesPerDay = med.TimesPerDay;
                    vm.IsOptional = med.IsOptional;
                    Medications.Add(vm);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading medications: {ex.Message}");
            }
        }
        
        private async Task LoadRestrictionsAsync()
        {
            Restrictions.Clear();
            
            try
            {
                var restrictions = await _calendarService.GetRestrictionsForDayAsync(DayNumber);
                
                foreach (var restriction in restrictions)
                {
                    var vm = new DayRestrictionViewModel();
                    vm.Name = restriction.Name;
                    vm.Description = restriction.Description;
                    vm.Reason = restriction.Reason;
                    vm.IsCritical = restriction.IsCritical;
                    vm.RecommendedAlternative = restriction.RecommendedAlternative;
                    Restrictions.Add(vm);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading restrictions: {ex.Message}");
            }
        }
        
        private async Task LoadInstructionsAsync()
        {
            Instructions.Clear();
            
            try
            {
                var instructions = await _calendarService.GetInstructionsForDayAsync(DayNumber);
                
                foreach (var instruction in instructions)
                {
                    var vm = new DayInstructionViewModel();
                    vm.Name = instruction.Name;
                    vm.Description = instruction.Description;
                    vm.Steps = instruction.Steps;
                    Instructions.Add(vm);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading instructions: {ex.Message}");
            }
        }
        
        private async Task LoadWarningsAsync()
        {
            Warnings.Clear();
            
            try
            {
                var warnings = await _calendarService.GetWarningsForDayAsync(DayNumber);
                
                foreach (var warning in warnings)
                {
                    var vm = new DayWarningViewModel();
                    vm.Name = warning.Name;
                    vm.Description = warning.Description;
                    Warnings.Add(vm);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading warnings: {ex.Message}");
            }
        }
    }
} 