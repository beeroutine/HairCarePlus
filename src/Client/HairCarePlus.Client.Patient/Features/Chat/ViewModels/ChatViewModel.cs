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
    public CollectionView MessagesCollectionView { get; set; } = default!;
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
    private string _messageText = string.Empty;

    [ObservableProperty]
    private ChatMessage? _replyToMessage = null;

    [ObservableProperty]
    private ChatMessage? _editingMessage = null;

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
        catch (Exception /*ex*/)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load chat messages", "OK");
            }
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
                await SimulateDoctorResponseAsync();
            }
            
            await ScrollToBottom();
        }
        catch (Exception /*ex*/)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to send message", "OK");
            }
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
        catch (Exception /*ex*/)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete message", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task HandleReplyToMessage(ChatMessage message)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"=== HandleReplyToMessage вызван: {message?.Content}");
        System.Diagnostics.Debug.WriteLine($"=== SenderId: {message?.SenderId}");
#endif
        
        if (message == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== ОШИБКА: message == null");
#endif
            return;
        }
        
        if (EditingMessage != null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== ОТМЕНА: EditingMessage != null");
#endif
            return;
        }
        
        if (message.SenderId == "patient")
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== ОТМЕНА: SenderId == patient");
#endif
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Информация", "Нельзя ответить на собственное сообщение", "OK");
            }
            return;
        }
        
        try 
        {
            ReplyToMessage = message;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"=== ReplyToMessage установлен: {ReplyToMessage?.Content}");
#endif
            
            await ScrollToBottom();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"=== ОШИБКА: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"=== {ex.StackTrace}");
#endif
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
        
        ReplyToMessage = null;
        
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
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Coming Soon", "Camera functionality will be available soon", "OK");
        }
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Coming Soon", "Photo picker functionality will be available soon", "OK");
        }
    }

    [RelayCommand]
    private void HideKeyboard()
    {
        _keyboardService?.HideKeyboard();
    }
    
    private async Task SimulateDoctorResponseAsync()
    {
        try
        {
            await Task.Delay(_random.Next(1000, 3000));
            
            string responseContent = _doctorResponses[_random.Next(_doctorResponses.Length)];
            
            var latestPatientMessage = Messages.LastOrDefault(m => m.SenderId == "patient");
            bool shouldReply = _random.NextDouble() < 0.3 && latestPatientMessage != null;
            
            var doctorResponse = new ChatMessage
            {
                Content = responseContent,
                SenderId = "doctor",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = MessageStatus.Delivered,
                SyncStatus = SyncStatus.Synced,
                ReplyTo = shouldReply ? latestPatientMessage : null
            };
            
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.Dispatcher.DispatchAsync(() =>
                {
                    Messages.Add(doctorResponse);
                });
            }
        }
        catch (Exception)
        {
            // Ошибки симуляции игнорируем
        }
    }

    private async Task ScrollToBottom()
    {
        try
        {
            if (MessagesCollectionView?.ItemsSource != null && Messages.Any())
            {
                await Task.Delay(50);
                MessagesCollectionView.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
#endif
        }
    }
} 