using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Profile.Models;
using HairCarePlus.Client.Patient.Features.Profile.Services;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.Profile.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly IProfileService _profileService;
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private PatientProfile? _profile;
        private ObservableCollection<DayViewModel> _weekDays;
        private ObservableCollection<TaskViewModel> _dailyTasks;
        private DateTime _selectedDate;
        private string _newMedication = string.Empty;
        private string _newAllergy = string.Empty;
        private bool _isEditing;

        public ProfileViewModel(
            IProfileService profileService,
            INavigationService navigationService,
            IVibrationService vibrationService)
        {
            _profileService = profileService;
            _navigationService = navigationService;
            _vibrationService = vibrationService;
            _weekDays = new ObservableCollection<DayViewModel>();
            _dailyTasks = new ObservableCollection<TaskViewModel>();
            _selectedDate = DateTime.Today;

            SaveCommand = new Command(async () => await SaveProfileAsync(), () => IsEditing);
            EditCommand = new Command(() => IsEditing = true);
            CancelCommand = new Command(() => CancelEditing());
            UploadPhotoCommand = new Command(async () => await UploadPhotoAsync());
            AddMedicationCommand = new Command(async () => await AddMedicationAsync(), () => !string.IsNullOrEmpty(NewMedication));
            RemoveMedicationCommand = new Command<string>(async (med) => await RemoveMedicationAsync(med));
            AddAllergyCommand = new Command(async () => await AddAllergyAsync(), () => !string.IsNullOrEmpty(NewAllergy));
            RemoveAllergyCommand = new Command<string>(async (allergy) => await RemoveAllergyAsync(allergy));
            SyncCommand = new Command(async () => await SyncProfileAsync());
            SelectDateCommand = new Command<DayViewModel>(OnDateSelected);
            TaskSelectedCommand = new Command<TaskViewModel>(OnTaskSelected);
            CompleteTaskCommand = new Command<TaskViewModel>(OnCompleteTask);
            DeleteTaskCommand = new Command<TaskViewModel>(OnDeleteTask);

            Title = "My Profile";
            InitializeWeekDays();
        }

        public PatientProfile? Profile
        {
            get => _profile;
            private set => SetProperty(ref _profile, value);
        }

        public ObservableCollection<DayViewModel> WeekDays
        {
            get => _weekDays;
            set => SetProperty(ref _weekDays, value);
        }

        public ObservableCollection<TaskViewModel> DailyTasks
        {
            get => _dailyTasks;
            set => SetProperty(ref _dailyTasks, value);
        }

        public string NewMedication
        {
            get => _newMedication;
            set
            {
                SetProperty(ref _newMedication, value);
                (AddMedicationCommand as Command)?.ChangeCanExecute();
            }
        }

        public string NewAllergy
        {
            get => _newAllergy;
            set
            {
                SetProperty(ref _newAllergy, value);
                (AddAllergyCommand as Command)?.ChangeCanExecute();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                SetProperty(ref _isEditing, value);
                (SaveCommand as Command)?.ChangeCanExecute();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand UploadPhotoCommand { get; }
        public ICommand AddMedicationCommand { get; }
        public ICommand RemoveMedicationCommand { get; }
        public ICommand AddAllergyCommand { get; }
        public ICommand RemoveAllergyCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand TaskSelectedCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                Profile = await _profileService.GetProfileAsync();
                await LoadDailyTasksAsync();
            });
        }

        private void InitializeWeekDays()
        {
            var today = DateTime.Today;
            WeekDays.Clear();
            for (int i = -3; i <= 3; i++)
            {
                var date = today.AddDays(i);
                WeekDays.Add(new DayViewModel
                {
                    Date = date.Day.ToString(),
                    DayOfWeek = date.ToString("ddd"),
                    FullDate = date,
                    IsSelected = i == 0
                });
            }
        }

        private async void OnDateSelected(DayViewModel day)
        {
            if (day == null) return;

            foreach (var d in WeekDays)
            {
                d.IsSelected = d == day;
            }

            _selectedDate = day.FullDate;
            await LoadDailyTasksAsync();
        }

        private async Task LoadDailyTasksAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Here you would typically load tasks from your service
                // For now, we'll add some sample tasks
                DailyTasks.Clear();
                DailyTasks.Add(new TaskViewModel
                {
                    Title = "Take Medication",
                    Instructions = "2 pills after breakfast",
                    DueTime = DateTime.Today.AddHours(9),
                    IsCompleted = false
                });
                DailyTasks.Add(new TaskViewModel
                {
                    Title = "Apply Shampoo",
                    Instructions = "Gentle massage for 5 minutes",
                    DueTime = DateTime.Today.AddHours(14),
                    IsCompleted = false
                });
            });
        }

        private void OnTaskSelected(TaskViewModel task)
        {
            // Handle task selection
            // You could navigate to a task details page here
        }

        private async Task SaveProfileAsync()
        {
            if (Profile == null) return;

            await ExecuteAsync(async () =>
            {
                Profile = await _profileService.UpdateProfileAsync(Profile);
                IsEditing = false;
            });

            if (_vibrationService.HasVibrator)
            {
                _vibrationService.Vibrate(100); // Короткая вибрация при сохранении
            }
        }

        private void CancelEditing()
        {
            IsEditing = false;
            LoadDataAsync().ConfigureAwait(false);
        }

        private async Task UploadPhotoAsync()
        {
            if (Profile == null) return;

            await ExecuteAsync(async () =>
            {
                try
                {
                    var photo = await MediaPicker.PickPhotoAsync();
                    if (photo != null)
                    {
                        using var stream = await photo.OpenReadAsync();
                        var photoUrl = await _profileService.UploadProfilePhotoAsync(stream, photo.FileName);
                        Profile.PhotoUrl = photoUrl;
                        await SaveProfileAsync();
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Failed to upload photo: " + ex.Message;
                }
            });
        }

        private async Task AddMedicationAsync()
        {
            if (string.IsNullOrEmpty(NewMedication)) return;

            await ExecuteAsync(async () =>
            {
                await _profileService.AddMedicationAsync(NewMedication);
                NewMedication = string.Empty;
                await LoadDataAsync();
            });
        }

        private async Task RemoveMedicationAsync(string medication)
        {
            await ExecuteAsync(async () =>
            {
                await _profileService.RemoveMedicationAsync(medication);
                await LoadDataAsync();
            });
        }

        private async Task AddAllergyAsync()
        {
            if (string.IsNullOrEmpty(NewAllergy)) return;

            await ExecuteAsync(async () =>
            {
                await _profileService.AddAllergyAsync(NewAllergy);
                NewAllergy = string.Empty;
                await LoadDataAsync();
            });
        }

        private async Task RemoveAllergyAsync(string allergy)
        {
            await ExecuteAsync(async () =>
            {
                await _profileService.RemoveAllergyAsync(allergy);
                await LoadDataAsync();
            });
        }

        private async Task SyncProfileAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _profileService.SyncProfileAsync();
                await LoadDataAsync();
            });
        }

        private void OnCompleteTask(TaskViewModel task)
        {
            if (task == null) return;

            task.IsCompleted = true;
            _vibrationService.Vibrate(50); // Короткая вибрация при выполнении задачи
        }

        private void OnDeleteTask(TaskViewModel task)
        {
            if (task == null) return;

            DailyTasks.Remove(task);
            _vibrationService.Vibrate(100); // Вибрация при удалении задачи
        }
    }

    public class DayViewModel : ViewModelBase
    {
        private string _date;
        private string _dayOfWeek;
        private DateTime _fullDate;
        private bool _isSelected;

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
    }

    public class TaskViewModel : ViewModelBase
    {
        private string _title;
        private string _instructions;
        private DateTime _dueTime;
        private bool _isCompleted;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Instructions
        {
            get => _instructions;
            set => SetProperty(ref _instructions, value);
        }

        public DateTime DueTime
        {
            get => _dueTime;
            set => SetProperty(ref _dueTime, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
    }
} 