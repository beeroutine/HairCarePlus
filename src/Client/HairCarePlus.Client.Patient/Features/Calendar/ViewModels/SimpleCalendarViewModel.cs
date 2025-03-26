using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    public class SimpleCalendarViewModel : BaseViewModel
    {
        private DateTime _currentMonthDate;
        private DateTime _selectedDate;
        private bool _isMonthViewVisible = true;
        
        // Событие для явного уведомления об изменении месяца
        public event EventHandler CurrentMonthChanged;

        public DateTime CurrentMonthDate
        {
            get => _currentMonthDate;
            set 
            {
                if (SetProperty(ref _currentMonthDate, value))
                {
                    // Явно уведомляем о смене месяца
                    CurrentMonthChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string CurrentMonthYear => CurrentMonthDate.ToString("MMMM yyyy");
        
        public string SelectedDateText => SelectedDate.ToString("d MMMM");

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }
        
        public bool IsMonthViewVisible
        {
            get => _isMonthViewVisible;
            set => SetProperty(ref _isMonthViewVisible, value);
        }

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand DaySelectedCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand BackToMonthViewCommand { get; }

        public SimpleCalendarViewModel()
        {
            Title = "Calendar";
            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;

            PreviousMonthCommand = new Command(ExecutePreviousMonth);
            NextMonthCommand = new Command(ExecuteNextMonth);
            DaySelectedCommand = new Command<DateTime>(ExecuteDaySelected);
            GoToTodayCommand = new Command(ExecuteGoToToday);
            BackToMonthViewCommand = new Command(ExecuteBackToMonthView);
        }

        private void ExecutePreviousMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(-1);
        }

        private void ExecuteNextMonth()
        {
            CurrentMonthDate = CurrentMonthDate.AddMonths(1);
        }

        private void ExecuteDaySelected(DateTime date)
        {
            SelectedDate = date;
            IsMonthViewVisible = false;
        }

        private void ExecuteGoToToday()
        {
            CurrentMonthDate = DateTime.Today;
            SelectedDate = DateTime.Today;
        }
        
        private void ExecuteBackToMonthView()
        {
            IsMonthViewVisible = true;
        }
    }
} 