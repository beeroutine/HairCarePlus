using System.Collections.ObjectModel;
using System.Windows.Input;
using HairCarePlus.Client.Patient.Common;
using HairCarePlus.Client.Patient.Features.Doctor.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.Doctor.ViewModels
{
    public class DoctorChatViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IVibrationService _vibrationService;
        private bool _isLoading;
        private string _messageText;
        private Models.Doctor _doctor;
        private ObservableCollection<ChatMessage> _messages;

        public DoctorChatViewModel(
            INavigationService navigationService,
            IVibrationService vibrationService)
        {
            _navigationService = navigationService;
            _vibrationService = vibrationService;
            _messages = new ObservableCollection<ChatMessage>();

            Title = "Chat with Doctor";

            SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(MessageText));
            AttachPhotoCommand = new Command(async () => await AttachPhotoAsync());
            RequestAppointmentCommand = new Command(async () => await RequestAppointmentAsync());
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        public Models.Doctor Doctor
        {
            get => _doctor;
            set => SetProperty(ref _doctor, value);
        }

        public string MessageText
        {
            get => _messageText;
            set
            {
                SetProperty(ref _messageText, value);
                (SendMessageCommand as Command)?.ChangeCanExecute();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SendMessageCommand { get; }
        public ICommand AttachPhotoCommand { get; }
        public ICommand RequestAppointmentCommand { get; }

        public override async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    // TODO: Загрузка данных о враче и истории сообщений
                    await Task.Delay(1000); // Имитация загрузки

                    Doctor = new Models.Doctor
                    {
                        Id = "1",
                        Name = "Dr. Sarah Johnson",
                        Specialty = "Hair Transplant Specialist",
                        PhotoUrl = "doctor_photo.jpg",
                        IsOnline = true
                    };

                    Messages = new ObservableCollection<ChatMessage>
                    {
                        new ChatMessage
                        {
                            Id = "1",
                            SenderId = Doctor.Id,
                            SenderName = Doctor.Name,
                            Content = "Hello! How can I help you today?",
                            Timestamp = DateTime.Now.AddMinutes(-30),
                            Type = MessageType.Text,
                            IsRead = true
                        }
                    };
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "patient", // TODO: Получить ID пациента
                SenderName = "You",
                Content = MessageText,
                Timestamp = DateTime.Now,
                Type = MessageType.Text,
                IsSending = true
            };

            Messages.Add(message);
            _vibrationService.Vibrate(50); // Короткая вибрация при отправке

            MessageText = string.Empty;

            // Имитация отправки сообщения
            await Task.Delay(1000);
            message.IsSending = false;

            // Имитация ответа врача
            await Task.Delay(2000);
            Messages.Add(new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = Doctor.Id,
                SenderName = Doctor.Name,
                Content = "I'll check your progress and get back to you shortly.",
                Timestamp = DateTime.Now,
                Type = MessageType.Text
            });

            _vibrationService.VibrationPattern(new long[] { 0, 100, 50, 100 }); // Вибрация при получении ответа
        }

        private async Task AttachPhotoAsync()
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
                        SenderName = "You",
                        Content = "Photo attachment",
                        AttachmentUrl = photo.FullPath,
                        Timestamp = DateTime.Now,
                        Type = MessageType.Photo,
                        IsSending = true
                    };

                    Messages.Add(message);
                    _vibrationService.Vibrate(50);

                    // Имитация загрузки фото
                    await Task.Delay(2000);
                    message.IsSending = false;
                }
            }
            catch (Exception ex)
            {
                // TODO: Обработка ошибок
                System.Diagnostics.Debug.WriteLine($"Error attaching photo: {ex.Message}");
            }
        }

        private async Task RequestAppointmentAsync()
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                DateTime = DateTime.Now.AddDays(1),
                Purpose = "Follow-up consultation",
                Status = AppointmentStatus.Requested
            };

            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "patient",
                SenderName = "You",
                Content = $"Appointment requested for {appointment.DateTime:g}",
                Timestamp = DateTime.Now,
                Type = MessageType.Appointment
            };

            Messages.Add(message);
            _vibrationService.Vibrate(100);

            await _navigationService.NavigateToAsync("appointment/details", new Dictionary<string, object>
            {
                { "appointmentId", appointment.Id }
            });
        }
    }
} 