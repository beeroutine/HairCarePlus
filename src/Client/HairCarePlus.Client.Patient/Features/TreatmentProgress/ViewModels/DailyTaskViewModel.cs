using HairCarePlus.Client.Patient.Common;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public class DailyTaskViewModel : ViewModelBase
    {
        private string _title;
        private string _description;
        private DateTime _time;
        private bool _isCompleted;

        public string Title
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