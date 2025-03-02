using HairCarePlus.Client.Patient.Common;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public class DayViewModel : ViewModelBase
    {
        private string _date = string.Empty;
        private string _dayOfWeek = string.Empty;
        private DateTime _fullDate;
        private bool _isSelected;
        private bool _hasTasks;
        private bool _hasEvents;
        private bool _isToday;
        private int _taskCount;
        private string _taskTypes = string.Empty;

        public DayViewModel()
        {
            _date = string.Empty;
            _dayOfWeek = string.Empty;
            _taskTypes = string.Empty;
            _fullDate = DateTime.Today;
        }

        public string Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string DayOfWeek
        {
            get => _dayOfWeek;
            set => SetProperty(ref _dayOfWeek, value);
        }

        public DateTime FullDate
        {
            get => _fullDate;
            set => SetProperty(ref _fullDate, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsToday
        {
            get => _isToday;
            set => SetProperty(ref _isToday, value);
        }

        public bool HasTasks
        {
            get => _hasTasks;
            set => SetProperty(ref _hasTasks, value);
        }

        public bool HasEvents
        {
            get => _hasEvents;
            set => SetProperty(ref _hasEvents, value);
        }

        public int TaskCount
        {
            get => _taskCount;
            set => SetProperty(ref _taskCount, value);
        }

        public string TaskTypes
        {
            get => _taskTypes;
            set => SetProperty(ref _taskTypes, value);
        }
    }
} 