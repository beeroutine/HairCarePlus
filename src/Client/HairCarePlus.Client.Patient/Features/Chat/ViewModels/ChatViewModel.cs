using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using Microsoft.Maui.Controls;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalStorageService _localStorageService;
    private readonly IKeyboardService _keyboardService;
    private readonly IChatRepository _chatRepository;
    private readonly Random _random = new Random();
    public CollectionView MessagesCollectionView { get; set; }
    private readonly string[] _doctorResponses = new[]
    {
        "How are you feeling today? It's important to monitor any discomfort during the recovery period.",
        "Remember to follow the post-operative care instructions for best results.",
        "Don't hesitate to send me photos if you notice anything unusual.",
        "Make sure you're applying the prescribed ointment as directed.",
        "Are you experiencing any swelling or inflammation?",
        "It's normal to have some mild discomfort during the first week post-transplant.",
        "Have you been washing your hair according to the guidelines I provided?",
        "Your recovery seems to be progressing well based on what you've shared.",
        "You can expect the transplanted hair to shed within 2-3 weeks, don't be alarmed when this happens.",
        "Are you taking your prescribed medications regularly?"
    };

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages;

    [ObservableProperty]
    private string _messageText;

    [ObservableProperty]
    private ChatMessage _replyToMessage;

    [ObservableProperty]
    private ChatMessage _editingMessage;

    [ObservableProperty]
    private Doctor _doctor;

    public ChatViewModel(
        INavigationService navigationService,
        ILocalStorageService localStorageService,
        IKeyboardService keyboardService,
        IChatRepository chatRepository)
    {
        _navigationService = navigationService;
        _localStorageService = localStorageService;
        _keyboardService = keyboardService;
        _chatRepository = chatRepository;
        Messages = new ObservableCollection<ChatMessage>();
        
        // Initialize doctor for display
        Doctor = new Doctor
        {
            Id = "doctor1",
            Name = "Dr. Sarah Johnson",
            Specialty = "Hair Transplant Specialist",
            PhotoUrl = "doctor_profile.png",
            IsOnline = true,
            LastSeen = DateTime.Now
        };
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            Messages.Clear();
            var messages = await _chatRepository.GetMessagesAsync("default_conversation");
            foreach (var msg in messages.OrderBy(m => m.SentAt))
                Messages.Add(msg);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to load chat messages", "OK");
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;
        try
        {
            if (EditingMessage != null)
            {
                EditingMessage.Content = MessageText;
                await _chatRepository.UpdateMessageAsync(EditingMessage);
                int index = Messages.IndexOf(EditingMessage);
                if (index >= 0)
                {
                    Messages[index] = EditingMessage;
                }
                MessageText = string.Empty;
                EditingMessage = null;
            }
            else
            {
                var message = new ChatMessage
                {
                    Content = MessageText,
                    SenderId = "patient",
                    ConversationId = "default_conversation",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Sending,
                    SyncStatus = SyncStatus.NotSynced,
                    ReplyTo = ReplyToMessage
                };
                var localId = await _chatRepository.SaveMessageAsync(message);
                message.LocalId = localId;
                Messages.Add(message);
                MessageText = string.Empty;
                ReplyToMessage = null;
                // Optionally: trigger sync service here
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to send message", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteMessage(ChatMessage message)
    {
        if (message == null) return;
        try
        {
            await _chatRepository.DeleteMessageAsync(message.LocalId);
            Messages.Remove(message);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete message", "OK");
        }
    }

    [RelayCommand]
    private async Task HandleReplyToMessage(ChatMessage message)
    {
        // Добавляем логирование для отладки
        System.Diagnostics.Debug.WriteLine($"=== HandleReplyToMessage вызван: {message?.Content}");
        System.Diagnostics.Debug.WriteLine($"=== SenderId: {message?.SenderId}");
        
        if (message == null)
        {
            System.Diagnostics.Debug.WriteLine("=== ОШИБКА: message == null");
            return;
        }
        
        // Нельзя отвечать на сообщение при редактировании
        if (EditingMessage != null)
        {
            System.Diagnostics.Debug.WriteLine("=== ОТМЕНА: EditingMessage != null");
            return;
        }
        
        // Проверка, что это сообщение от доктора (нельзя отвечать на свои сообщения)
        if (message.SenderId == "patient")
        {
            System.Diagnostics.Debug.WriteLine("=== ОТМЕНА: SenderId == patient");
            await Application.Current.MainPage.DisplayAlert("Информация", "Нельзя ответить на собственное сообщение", "OK");
            return;
        }
        
        try 
        {
            // Устанавливаем сообщение для ответа
            ReplyToMessage = message;
            System.Diagnostics.Debug.WriteLine($"=== ReplyToMessage установлен: {ReplyToMessage?.Content}");
            
            // Прокрутка списка к концу
            await ScrollToBottom();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ОШИБКА: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"=== {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private void CancelReply()
    {
        ReplyToMessage = null;
    }

    [RelayCommand]
    private void EditMessage(ChatMessage message)
    {
        if (message == null || message.SenderId != "patient") return;
        
        // Отменяем режим ответа при редактировании
        ReplyToMessage = null;
        
        // Включаем режим редактирования
        EditingMessage = message;
        MessageText = message.Content;
    }

    [RelayCommand]
    private void CancelEditCommand()
    {
        EditingMessage = null;
        MessageText = string.Empty;
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("//today");
    }

    [RelayCommand]
    private async Task OpenCamera()
    {
        // TODO: Implement camera functionality
        await Application.Current.MainPage.DisplayAlert("Coming Soon", "Camera functionality will be available soon", "OK");
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        // TODO: Implement photo picker functionality
        await Application.Current.MainPage.DisplayAlert("Coming Soon", "Photo picker functionality will be available soon", "OK");
    }

    [RelayCommand]
    private void HideKeyboard()
    {
        _keyboardService?.HideKeyboard();
    }
    
    // Симуляция ответа от врача
    private async Task SimulateDoctorResponseAsync()
    {
        try
        {
            // Задержка перед ответом (1-3 секунды)
            await Task.Delay(_random.Next(1000, 3000));
            
            // Выбор случайного ответа
            string responseContent = _doctorResponses[_random.Next(_doctorResponses.Length)];
            
            // 30% вероятность ответа на последнее сообщение пациента
            var latestPatientMessage = Messages.LastOrDefault(m => m.SenderId == "patient");
            bool shouldReply = _random.NextDouble() < 0.3 && latestPatientMessage != null;
            
            var doctorResponse = new ChatMessage
            {
                Content = responseContent,
                SenderId = "doctor",
                Timestamp = DateTime.Now,
                ReplyTo = shouldReply ? latestPatientMessage : null
            };
            
            // Добавляем ответ врача в основном потоке
            await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
            {
                Messages.Add(doctorResponse);
            });
        }
        catch (Exception)
        {
            // Ошибки симуляции игнорируем
        }
    }

    // Метод для прокрутки к концу списка сообщений
    private async Task ScrollToBottom()
    {
        try
        {
            if (MessagesCollectionView != null && Messages.Count > 0)
            {
                // Прокрутка к последнему сообщению
                await Task.Delay(100); // Небольшая задержка, чтобы UI обновился
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                    MessagesCollectionView.ScrollTo(Messages.Last(), animate: true);
                });
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки при прокрутке
        }
    }
} 