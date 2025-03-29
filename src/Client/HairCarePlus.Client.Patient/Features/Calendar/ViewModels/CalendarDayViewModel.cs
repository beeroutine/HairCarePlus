using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Text;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _hasEvents;
        private bool _isSelected;
        private bool _isToday;
        private bool _hasMedication;
        private bool _hasPhoto;
        private bool _hasRestriction;
        private bool _hasInstruction;
        private int _totalEvents;
        private bool _isPartOfMedicationRange;
        private bool _isPartOfRestrictionRange;
        private bool _isFirstDayInMedicationRange;
        private bool _isLastDayInMedicationRange;
        private bool _isFirstDayInRestrictionRange;
        private bool _isLastDayInRestrictionRange;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private bool _isBusy;
        private readonly ILogger<CalendarDayViewModel>? _logger;
        private List<CalendarEvent>? _dayEvents;
        private List<CalendarEvent>? _monthEvents;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                if (_isCurrentMonth != value)
                {
                    _isCurrentMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasEvents
        {
            get => _hasEvents;
            set
            {
                if (_hasEvents != value)
                {
                    _hasEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsToday
        {
            get => _isToday;
            set
            {
                if (_isToday != value)
                {
                    _isToday = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasMedication
        {
            get => _hasMedication;
            set
            {
                if (_hasMedication != value)
                {
                    _hasMedication = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasPhoto
        {
            get => _hasPhoto;
            set
            {
                if (_hasPhoto != value)
                {
                    _hasPhoto = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasRestriction
        {
            get => _hasRestriction;
            set
            {
                if (_hasRestriction != value)
                {
                    _hasRestriction = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasInstruction
        {
            get => _hasInstruction;
            set
            {
                if (_hasInstruction != value)
                {
                    _hasInstruction = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalEvents
        {
            get => _totalEvents;
            set
            {
                if (_totalEvents != value)
                {
                    _totalEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasExcessEvents => TotalEvents > 3;
        public int ExcessEventsCount => TotalEvents > 3 ? TotalEvents - 3 : 0;
        public int DayNumber => Date.Day;
        public string DayText => Date.Day.ToString();

        public bool IsPartOfMedicationRange
        {
            get => _isPartOfMedicationRange;
            set
            {
                if (_isPartOfMedicationRange != value)
                {
                    _isPartOfMedicationRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPartOfRestrictionRange
        {
            get => _isPartOfRestrictionRange;
            set
            {
                if (_isPartOfRestrictionRange != value)
                {
                    _isPartOfRestrictionRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFirstDayInMedicationRange
        {
            get => _isFirstDayInMedicationRange;
            set
            {
                if (_isFirstDayInMedicationRange != value)
                {
                    _isFirstDayInMedicationRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLastDayInMedicationRange
        {
            get => _isLastDayInMedicationRange;
            set
            {
                if (_isLastDayInMedicationRange != value)
                {
                    _isLastDayInMedicationRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFirstDayInRestrictionRange
        {
            get => _isFirstDayInRestrictionRange;
            set
            {
                if (_isFirstDayInRestrictionRange != value)
                {
                    _isFirstDayInRestrictionRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLastDayInRestrictionRange
        {
            get => _isLastDayInRestrictionRange;
            set
            {
                if (_isLastDayInRestrictionRange != value)
                {
                    _isLastDayInRestrictionRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private async Task RefreshCalendarAsync()
        {
            try
            {
                _logger?.LogInformation("Начало RefreshCalendarAsync");
                HasError = false;
                ErrorMessage = string.Empty;
                IsBusy = true;
                
                // Инициализируем данные, если они еще не загружены
                if (_monthEvents == null)
                {
                    await LoadMonthEventsAsync();
                }
                
                if (_dayEvents == null)
                {
                    await LoadDayEventsAsync();
                }
                
                _logger?.LogInformation("Calendar refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing calendar");
                HasError = true;
                ErrorMessage = "Произошла ошибка при загрузке календаря. Пожалуйста, попробуйте снова.";
                
                // Логируем ошибку для отладки
                System.Diagnostics.Debug.WriteLine($"ОШИБКА КАЛЕНДАРЯ: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null) 
                {
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                    _logger?.LogError(ex.InnerException, "Inner exception from refreshing calendar");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadMonthEventsAsync()
        {
            if (_monthEvents != null)
            {
                // Уже загружены
                return;
            }
            
            try
            {
                IsBusy = true;
                HasError = false;

                // Примечание: Для полной интеграции здесь нужно добавить ICalendarService
                // через конструктор CalendarDayViewModel.
                // Сейчас используем моки для тестирования
                
                // Используем тестовые данные вместо реального сервиса
                _monthEvents = GenerateTestMonthEvents();
                
                // Обновляем свойства на основе полученных данных
                UpdateMonthProperties();
                
                _logger?.LogInformation($"Loaded {_monthEvents?.Count ?? 0} events for month {Date.Year}-{Date.Month}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading month events");
                HasError = true;
                ErrorMessage = "Не удалось загрузить события месяца.";
                throw; // Пробрасываем исключение дальше для обработки в RefreshCalendarAsync
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadDayEventsAsync()
        {
            if (_dayEvents != null)
            {
                // Уже загружены
                return;
            }
            
            try
            {
                IsBusy = true;
                HasError = false;

                // Примечание: Для полной интеграции здесь нужно добавить ICalendarService
                // через конструктор CalendarDayViewModel.
                // Сейчас используем моки для тестирования
                
                // Используем тестовые данные вместо реального сервиса
                _dayEvents = GenerateTestDayEvents();
                
                // Обновляем свойства на основе полученных данных
                UpdateDayProperties();
                
                _logger?.LogInformation($"Loaded {_dayEvents?.Count ?? 0} events for date {Date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading day events");
                HasError = true;
                ErrorMessage = "Не удалось загрузить события дня.";
                throw; // Пробрасываем исключение дальше для обработки в RefreshCalendarAsync
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateMonthProperties()
        {
            if (_monthEvents == null || !_monthEvents.Any())
            {
                HasEvents = false;
                return;
            }

            HasEvents = true;
            
            // Определяем, является ли текущий день частью диапазона ограничений или медикаментов
            IsPartOfMedicationRange = _monthEvents.Any(e => 
                e.EventType == EventType.MedicationTreatment && 
                e.Date <= Date && 
                (e.ExpirationDate == null || e.ExpirationDate >= Date));
                
            IsPartOfRestrictionRange = _monthEvents.Any(e => 
                e.EventType == EventType.CriticalWarning && 
                e.Date <= Date && 
                (e.ExpirationDate == null || e.ExpirationDate >= Date));
                
            // Определяем, является ли текущий день первым или последним в диапазоне
            IsFirstDayInMedicationRange = _monthEvents.Any(e => 
                e.EventType == EventType.MedicationTreatment && 
                e.Date.Date == Date.Date);
                
            IsLastDayInMedicationRange = _monthEvents.Any(e => 
                e.EventType == EventType.MedicationTreatment && 
                e.ExpirationDate.HasValue && 
                e.ExpirationDate.Value.Date == Date.Date);
                
            IsFirstDayInRestrictionRange = _monthEvents.Any(e => 
                e.EventType == EventType.CriticalWarning && 
                e.Date.Date == Date.Date);
                
            IsLastDayInRestrictionRange = _monthEvents.Any(e => 
                e.EventType == EventType.CriticalWarning && 
                e.ExpirationDate.HasValue && 
                e.ExpirationDate.Value.Date == Date.Date);
        }

        private void UpdateDayProperties()
        {
            if (_dayEvents == null || !_dayEvents.Any())
            {
                HasEvents = false;
                HasMedication = false;
                HasPhoto = false;
                HasRestriction = false;
                HasInstruction = false;
                TotalEvents = 0;
                return;
            }

            HasEvents = true;
            TotalEvents = _dayEvents.Count;
            
            // Определяем типы событий
            HasMedication = _dayEvents.Any(e => e.EventType == EventType.MedicationTreatment);
            HasPhoto = _dayEvents.Any(e => e.EventType == EventType.Photo);
            HasRestriction = _dayEvents.Any(e => e.EventType == EventType.CriticalWarning);
            HasInstruction = _dayEvents.Any(e => e.EventType == EventType.VideoInstruction);
        }

        private List<CalendarEvent> GenerateTestMonthEvents()
        {
            // Генерируем тестовые данные для месяца
            var events = new List<CalendarEvent>();
            var startDate = new DateTime(Date.Year, Date.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
            
            // Добавляем ограничение на первую неделю месяца
            events.Add(new CalendarEvent
            {
                Id = 1,
                Title = "Ограничение физической активности",
                Description = "Избегайте интенсивных физических нагрузок",
                Date = startDate,
                ExpirationDate = startDate.AddDays(7),
                EventType = EventType.CriticalWarning,
                TimeOfDay = TimeOfDay.Morning
            });
            
            // Добавляем прием медикаментов на вторую неделю месяца
            events.Add(new CalendarEvent
            {
                Id = 2,
                Title = "Прием антибиотиков",
                Description = "По 1 таблетке 2 раза в день",
                Date = startDate.AddDays(8),
                ExpirationDate = startDate.AddDays(15),
                EventType = EventType.MedicationTreatment,
                TimeOfDay = TimeOfDay.Morning
            });
            
            return events;
        }

        private List<CalendarEvent> GenerateTestDayEvents()
        {
            // Генерируем тестовые данные для выбранного дня
            var events = new List<CalendarEvent>();
            
            // Добавляем события только если это текущий день
            if (Date.Date == DateTime.Today.Date)
            {
                events.Add(new CalendarEvent
                {
                    Id = 101,
                    Title = "Прием лекарства",
                    Description = "Антибиотик широкого спектра",
                    Date = Date,
                    EventType = EventType.MedicationTreatment,
                    TimeOfDay = TimeOfDay.Morning
                });
                
                events.Add(new CalendarEvent
                {
                    Id = 102,
                    Title = "Фотоотчет",
                    Description = "Сделать фото области пересадки",
                    Date = Date,
                    EventType = EventType.Photo,
                    TimeOfDay = TimeOfDay.Evening
                });
            }
            
            return events;
        }

        private async Task LoadTestDataAsync()
        {
            try {
                // Создаем тестовые данные
                var testEvents = new List<CalendarEvent>();
                
                // Добавляем несколько тестовых событий на сегодня
                var today = DateTime.Today;
                testEvents.Add(new CalendarEvent {
                    Id = 1,
                    Title = "Тестовое событие",
                    Date = today,
                    TimeOfDay = TimeOfDay.Morning,
                    EventType = EventType.MedicationTreatment
                });
                
                // Обновляем UI
                // Используйте эти данные вместо данных из сервиса
                
                Console.WriteLine("Тестовые данные загружены успешно");
                HasError = false;
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка при загрузке тестовых данных: {ex.Message}");
            }
        }

        public string GetDiagnosticInfo()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== CalendarDayViewModel Diagnostic Info ===");
            sb.AppendLine($"Date: {Date:yyyy-MM-dd}");
            sb.AppendLine($"IsCurrentMonth: {IsCurrentMonth}");
            sb.AppendLine($"IsToday: {IsToday}");
            sb.AppendLine($"IsSelected: {IsSelected}");
            sb.AppendLine($"HasEvents: {HasEvents}");
            sb.AppendLine($"TotalEvents: {TotalEvents}");
            sb.AppendLine($"HasError: {HasError}");
            sb.AppendLine($"ErrorMessage: {ErrorMessage}");
            sb.AppendLine($"IsBusy: {IsBusy}");
            
            sb.AppendLine("\nEvent Types:");
            sb.AppendLine($"HasMedication: {HasMedication}");
            sb.AppendLine($"HasPhoto: {HasPhoto}");
            sb.AppendLine($"HasRestriction: {HasRestriction}");
            sb.AppendLine($"HasInstruction: {HasInstruction}");
            
            sb.AppendLine("\nRange Properties:");
            sb.AppendLine($"IsPartOfMedicationRange: {IsPartOfMedicationRange}");
            sb.AppendLine($"IsPartOfRestrictionRange: {IsPartOfRestrictionRange}");
            sb.AppendLine($"IsFirstDayInMedicationRange: {IsFirstDayInMedicationRange}");
            sb.AppendLine($"IsLastDayInMedicationRange: {IsLastDayInMedicationRange}");
            sb.AppendLine($"IsFirstDayInRestrictionRange: {IsFirstDayInRestrictionRange}");
            sb.AppendLine($"IsLastDayInRestrictionRange: {IsLastDayInRestrictionRange}");
            
            sb.AppendLine("\nEvents Data:");
            
            if (_monthEvents != null)
            {
                sb.AppendLine($"Month Events Count: {_monthEvents.Count}");
                foreach (var evt in _monthEvents)
                {
                    sb.AppendLine($" - {evt.Date:yyyy-MM-dd}: {evt.Title} ({evt.EventType})");
                }
            }
            else
            {
                sb.AppendLine("Month Events: null");
            }
            
            if (_dayEvents != null)
            {
                sb.AppendLine($"Day Events Count: {_dayEvents.Count}");
                foreach (var evt in _dayEvents)
                {
                    sb.AppendLine($" - {evt.EventType}: {evt.Title}");
                }
            }
            else
            {
                sb.AppendLine("Day Events: null");
            }
            
            return sb.ToString();
        }
    }
} 