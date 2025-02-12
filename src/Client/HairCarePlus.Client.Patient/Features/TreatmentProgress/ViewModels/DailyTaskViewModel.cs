using HairCarePlus.Client.Patient.Common;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public class DailyTaskViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private DateTime _time;
        private bool _isCompleted;

        public DailyTaskViewModel()
        {
            _time = DateTime.Now;
        }

        public new string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
    }
} 