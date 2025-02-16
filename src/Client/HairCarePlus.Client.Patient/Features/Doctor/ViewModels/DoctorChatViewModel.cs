using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Doctor.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace HairCarePlus.Client.Patient.Features.Doctor.ViewModels
{
    public partial class DoctorChatViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILocalStorageService _localStorageService;
        private readonly IKeyboardService _keyboardService;

        [ObservableProperty]
        private DoctorProfile _doctor;

        [ObservableProperty]
        private string _messageText = string.Empty;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ICommand BackCommand { get; }

        public DoctorChatViewModel(
            INavigationService navigationService, 
            ILocalStorageService localStorageService,
            IKeyboardService keyboardService)
        {
            _navigationService = navigationService;
            _localStorageService = localStorageService;
            _keyboardService = keyboardService;

            // Mock data for demonstration
            Doctor = new DoctorProfile
            {
                Id = "1",
                Name = "Dr. Sarah Johnson",
                Specialty = "Hair Treatment Specialist",
                PhotoUrl = "doctor_avatar.png",
                IsOnline = true
            };

            BackCommand = new Command(async () => await GoBack());
        }

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                LoadMessages();
            });
        }

        private void LoadMessages()
        {
            // Здесь будет загрузка реальных сообщений из базы данных или сервера
        }

        [RelayCommand]
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageText))
                return;

            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "patient",
                Content = MessageText.Trim(),
                Timestamp = DateTime.Now,
                Type = MessageType.Text
            };

            Messages.Add(message);
            MessageText = string.Empty;

            // Здесь будет отправка сообщения на сервер
        }

        [RelayCommand]
        private async Task AttachPhoto()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo != null)
                {
                    var message = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        SenderId = "patient",
                        Content = "Photo",
                        AttachmentUrl = photo.FullPath,
                        Timestamp = DateTime.Now,
                        Type = MessageType.Image
                    };

                    Messages.Add(message);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to attach photo: " + ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void DismissKeyboard()
        {
            _keyboardService.HideKeyboard();
        }

        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("//progress");
        }
    }
} 