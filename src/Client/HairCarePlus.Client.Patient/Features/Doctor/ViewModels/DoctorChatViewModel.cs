using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Doctor.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

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
            var mockMessages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Id = "1",
                    SenderId = "doctor",
                    Content = "Hello! How can I help you today?",
                    Timestamp = DateTime.Now.AddMinutes(-30),
                    Type = MessageType.Text
                },
                new ChatMessage
                {
                    Id = "2",
                    SenderId = "patient",
                    Content = "Hi Dr. Johnson! I have a question about my treatment plan.",
                    Timestamp = DateTime.Now.AddMinutes(-29),
                    Type = MessageType.Text
                },
                new ChatMessage
                {
                    Id = "3",
                    SenderId = "doctor",
                    Content = "Of course! Please go ahead and ask.",
                    Timestamp = DateTime.Now.AddMinutes(-28),
                    Type = MessageType.Text
                }
            };

            foreach (var message in mockMessages)
            {
                Messages.Add(message);
            }
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

            // Simulate doctor's response after 1 second
            Task.Delay(1000).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var response = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        SenderId = "doctor",
                        Content = "I'm reviewing your message. I'll get back to you shortly.",
                        Timestamp = DateTime.Now,
                        Type = MessageType.Text
                    };
                    Messages.Add(response);
                });
            });
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
    }
} 