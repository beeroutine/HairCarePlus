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
        private PatientProfile? _profile;
        private string _newMedication = string.Empty;
        private string _newAllergy = string.Empty;
        private bool _isEditing;

        public ProfileViewModel(
            IProfileService profileService,
            INavigationService navigationService)
        {
            _profileService = profileService;
            _navigationService = navigationService;

            SaveCommand = new Command(async () => await SaveProfileAsync(), () => IsEditing);
            EditCommand = new Command(() => IsEditing = true);
            CancelCommand = new Command(() => CancelEditing());
            UploadPhotoCommand = new Command(async () => await UploadPhotoAsync());
            AddMedicationCommand = new Command(async () => await AddMedicationAsync(), () => !string.IsNullOrEmpty(NewMedication));
            RemoveMedicationCommand = new Command<string>(async (med) => await RemoveMedicationAsync(med));
            AddAllergyCommand = new Command(async () => await AddAllergyAsync(), () => !string.IsNullOrEmpty(NewAllergy));
            RemoveAllergyCommand = new Command<string>(async (allergy) => await RemoveAllergyAsync(allergy));
            SyncCommand = new Command(async () => await SyncProfileAsync());

            Title = "My Profile";
        }

        public PatientProfile? Profile
        {
            get => _profile;
            private set => SetProperty(ref _profile, value);
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

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                Profile = await _profileService.GetProfileAsync();
            });
        }

        private async Task SaveProfileAsync()
        {
            if (Profile == null) return;

            await ExecuteAsync(async () =>
            {
                Profile = await _profileService.UpdateProfileAsync(Profile);
                IsEditing = false;
            });
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
    }
} 