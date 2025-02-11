using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.TreatmentProgress.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.TreatmentProgress.ViewModels
{
    public class TreatmentProgressViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private Models.TreatmentProgress _progress;
        private bool _isLoading;

        public TreatmentProgressViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            
            // Команды
            TakePhotoCommand = new Command(async () => await TakePhotoAsync());
            ContactDoctorCommand = new Command(async () => await ContactDoctorAsync());
            ViewMilestoneCommand = new Command<TreatmentMilestone>(async (milestone) => await ViewMilestoneAsync(milestone));
            
            Title = "My Progress";
        }

        public Models.TreatmentProgress Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand TakePhotoCommand { get; }
        public ICommand ContactDoctorCommand { get; }
        public ICommand ViewMilestoneCommand { get; }

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    // TODO: Загрузка данных о прогрессе с сервера
                    await Task.Delay(1000); // Имитация загрузки
                    Progress = new Models.TreatmentProgress
                    {
                        SurgeryDate = DateTime.Now.AddDays(-30),
                        DaysSinceSurgery = 30,
                        CurrentPhase = "Growth Phase",
                        ProgressPercentage = 40,
                        NextAction = new NextAction
                        {
                            Title = "Weekly Photo",
                            Description = "Time to take your weekly progress photo",
                            DueDate = DateTime.Now.AddHours(2),
                            Type = ActionType.TakePhoto
                        }
                    };
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        private async Task TakePhotoAsync()
        {
            await _navigationService.NavigateToAsync("photo/capture");
        }

        private async Task ContactDoctorAsync()
        {
            await _navigationService.NavigateToAsync("doctor/chat");
        }

        private async Task ViewMilestoneAsync(TreatmentMilestone milestone)
        {
            if (milestone == null) return;
            await _navigationService.NavigateToAsync("milestone/details", new Dictionary<string, object>
            {
                { "milestone", milestone }
            });
        }
    }
} 