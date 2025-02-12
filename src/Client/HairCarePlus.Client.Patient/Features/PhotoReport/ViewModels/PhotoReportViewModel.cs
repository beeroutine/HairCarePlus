using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.PhotoReport.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.PhotoReport.ViewModels
{
    public class PhotoReportViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private bool _isLoading;
        private ObservableCollection<Models.PhotoReport> _photoReports;

        public PhotoReportViewModel(
            INavigationService navigationService,
            IVibrationService vibrationService)
        {
            _navigationService = navigationService;
            _vibrationService = vibrationService;
            _photoReports = new ObservableCollection<Models.PhotoReport>();

            Title = "Photo Reports";

            TakePhotoCommand = new Command(async () => await TakePhotoAsync());
            ViewPhotoCommand = new Command<Models.PhotoReport>(async (photo) => await ViewPhotoAsync(photo));
        }

        public ObservableCollection<Models.PhotoReport> PhotoReports
        {
            get => _photoReports;
            set => SetProperty(ref _photoReports, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand TakePhotoCommand { get; }
        public ICommand ViewPhotoCommand { get; }

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    // TODO: Загрузка фотоотчетов с сервера
                    await Task.Delay(1000); // Имитация загрузки
                    PhotoReports = new ObservableCollection<Models.PhotoReport>
                    {
                        new Models.PhotoReport
                        {
                            Id = "1",
                            Date = DateTime.Now.AddDays(-7),
                            PhotoUrl = "sample_photo.jpg",
                            Status = PhotoStatus.Analyzed.ToString(),
                            Analysis = "Hair growth detected in transplanted areas. +15% density increase.",
                            GrowthProgress = 15,
                            DoctorComment = "Good progress, continue with the prescribed care routine.",
                            IsAnalyzed = true
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
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo != null)
                {
                    _vibrationService.Vibrate(50); // Короткая вибрация при выборе фото

                    // TODO: Загрузка фото на сервер
                    var newReport = new Models.PhotoReport
                    {
                        Id = Guid.NewGuid().ToString(),
                        Date = DateTime.Now,
                        PhotoUrl = photo.FullPath,
                        Status = PhotoStatus.Uploading.ToString(),
                        Analysis = "Awaiting analysis...",
                        DoctorComment = "Pending review"
                    };

                    PhotoReports.Insert(0, newReport);
                    await AnalyzePhotoAsync(newReport);
                }
            }
            catch (Exception ex)
            {
                // TODO: Обработка ошибок
                System.Diagnostics.Debug.WriteLine($"Error taking photo: {ex.Message}");
            }
        }

        private async Task ViewPhotoAsync(Models.PhotoReport photo)
        {
            if (photo == null) return;

            await _navigationService.NavigateToAsync("photo/details", new Dictionary<string, object>
            {
                { "photoId", photo.Id }
            });
        }

        private async Task AnalyzePhotoAsync(Models.PhotoReport report)
        {
            await Task.Delay(2000); // Имитация анализа
            report.Status = PhotoStatus.Analyzing.ToString();

            await Task.Delay(3000); // Имитация анализа
            report.Status = PhotoStatus.Analyzed.ToString();
            report.Analysis = "Initial analysis complete. Processing growth patterns...";
            report.IsAnalyzed = true;

            _vibrationService.VibrationPattern(new long[] { 0, 100, 100, 100 }); // Вибрация при завершении анализа
        }
    }
} 