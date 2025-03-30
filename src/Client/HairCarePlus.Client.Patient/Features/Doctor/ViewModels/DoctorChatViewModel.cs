using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Doctor.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common.Behaviors;

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

        [ObservableProperty]
        private ChatMessage _replyToMessage;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ICommand BackCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand OpenCameraCommand { get; }
        public ICommand ChoosePhotoCommand { get; }
        public ICommand ReplyToMessageCommand { get; }
        public ICommand CancelReplyCommand { get; }
        public ICommand HideKeyboardCommand { get; }

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
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
            OpenCameraCommand = new AsyncRelayCommand(OpenCameraAsync);
            ChoosePhotoCommand = new AsyncRelayCommand(ChoosePhotoAsync);
            ReplyToMessageCommand = new RelayCommand<ChatMessage>(SetReplyMessage);
            CancelReplyCommand = new RelayCommand(CancelReply);
            HideKeyboardCommand = new Command(HideKeyboard);
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

        private void SetReplyMessage(ChatMessage message)
        {
            if (message != null)
            {
                ReplyToMessage = message;
            }
        }

        private void CancelReply()
        {
            ReplyToMessage = null;
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText))
                return;

            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = MessageText,
                SenderId = "patient",
                Timestamp = DateTime.Now,
                Type = MessageType.Text,
                ReplyTo = ReplyToMessage
            };

            Messages.Add(message);
            MessageText = string.Empty;
            ReplyToMessage = null;

            // Принудительно вызываем прокрутку к последнему сообщению
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var collectionView = Application.Current.MainPage.FindByName<CollectionView>("MessagesCollection");
                if (collectionView != null && collectionView.ItemsSource != null)
                {
                    var items = collectionView.ItemsSource.Cast<object>().ToList();
                    if (items.Any())
                    {
                        collectionView.ScrollTo(items.Last(), position: ScrollToPosition.End);
                    }
                }
            });

            // Имитация ответа от доктора
            await SimulateReplyAsync();
        }

        private async Task SimulateReplyAsync()
        {
            // Имитация "печатает..."
            await Task.Delay(Random.Shared.Next(1000, 3000));

            var responses = new Dictionary<string, string[]>
            {
                ["hello"] = new[] { 
                    "Hello! How can I help you today?",
                    "Hi! How are you feeling? Any concerns about your treatment?"
                },
                ["pain"] = new[] {
                    "Could you describe the pain level from 1 to 10? And where exactly do you feel it?",
                    "I understand you're experiencing discomfort. Is it constant or does it come and go?"
                },
                ["treatment"] = new[] {
                    "Your treatment is progressing well. Keep following the prescribed care routine.",
                    "Based on your progress, everything looks normal. Continue with the recommended procedures."
                },
                ["question"] = new[] {
                    "That's a good question. Let me explain in detail...",
                    "I'll be happy to clarify that for you."
                },
                ["default"] = new[] {
                    "I've received your message. Let me check and get back to you shortly.",
                    "Thank you for letting me know. I'll review this information.",
                    "I understand. Please continue following your treatment plan, and let me know if you have any specific concerns."
                }
            };

            string[] possibleResponses = responses["default"];
            string lowercaseMessage = MessageText.ToLower();

            foreach (var category in responses.Keys)
            {
                if (lowercaseMessage.Contains(category))
                {
                    possibleResponses = responses[category];
                    break;
                }
            }

            var reply = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = possibleResponses[Random.Shared.Next(possibleResponses.Length)],
                SenderId = "doctor",
                Timestamp = DateTime.Now,
                Type = MessageType.Text,
                ReplyTo = ReplyToMessage != null ? Messages.LastOrDefault() : null
            };

            Messages.Add(reply);

            // Шанс на дополнительный ответ
            if (Random.Shared.Next(100) < 30)
            {
                await Task.Delay(Random.Shared.Next(1500, 3000));
                var followUp = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "Also, don't forget to update your progress in the app regularly. It helps me monitor your treatment better.",
                    SenderId = "doctor",
                    Timestamp = DateTime.Now,
                    Type = MessageType.Text
                };
                Messages.Add(followUp);
            }
        }

        private async Task OpenCameraAsync()
        {
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        await AddPhotoMessageAsync(photo);
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error", 
                        "Camera is not available on this device.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to take photo: {ex.Message}", 
                    "OK");
            }
        }

        private async Task ChoosePhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null)
                {
                    await AddPhotoMessageAsync(photo);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to pick photo: {ex.Message}", 
                    "OK");
            }
        }

        private async Task AddPhotoMessageAsync(FileResult photo)
        {
            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "patient",
                Content = "Photo",
                AttachmentUrl = photo.FullPath,
                Timestamp = DateTime.Now,
                Type = MessageType.Image,
                ReplyTo = ReplyToMessage
            };

            Messages.Add(message);
            ReplyToMessage = null;

            // Scroll to the new message
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var collectionView = Application.Current.MainPage.FindByName<CollectionView>("MessagesCollection");
                if (collectionView != null && collectionView.ItemsSource != null)
                {
                    var items = collectionView.ItemsSource.Cast<object>().ToList();
                    if (items.Any())
                    {
                        collectionView.ScrollTo(items.Last(), position: ScrollToPosition.End);
                    }
                }
            });

            // Simulate doctor's response
            await SimulateReplyAsync();
        }

        private void HideKeyboard()
        {
            _keyboardService?.HideKeyboard();
        }

        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("//today");
        }
    }
} 