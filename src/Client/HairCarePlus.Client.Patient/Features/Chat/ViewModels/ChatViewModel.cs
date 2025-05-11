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
using Microsoft.Extensions.Logging;
using ChatCommands = HairCarePlus.Client.Patient.Features.Chat.Application.Commands;
using ChatQueries = HairCarePlus.Client.Patient.Features.Chat.Application.Queries;
using HairCarePlus.Shared.Common.CQRS;
using MauiApp = Microsoft.Maui.Controls.Application;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace HairCarePlus.Client.Patient.Features.Chat.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalStorageService _localStorageService;
    private readonly IKeyboardService _keyboardService;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly IMessenger _messenger;
    private readonly Random _random = new Random();
    private readonly ILogger<ChatViewModel> _logger;
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
        ICommandBus commandBus,
        IQueryBus queryBus,
        IMessenger messenger,
        ILogger<ChatViewModel> logger)
    {
        _navigationService = navigationService;
        _localStorageService = localStorageService;
        _keyboardService = keyboardService;
        _commandBus = commandBus;
        _queryBus = queryBus;
        _messenger = messenger;
        _logger = logger;
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

        // Listen for captured photos
        _messenger.Register<PhotoCapturedMessage>(this, async (recipient, msg) =>
        {
            _logger.LogInformation("Received PhotoCapturedMessage, path={Path}", msg.Value);
            _logger.LogInformation("File exists on device: {Exists}", System.IO.File.Exists(msg.Value));
            await SendPhotoMessageAsync(msg.Value);
        });
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            Messages.Clear();
            var messages = await _queryBus.SendAsync<IReadOnlyList<ChatMessage>>(new ChatQueries.GetChatMessagesQuery("default_conversation"));
            foreach (var msg in messages.OrderBy(m => m.SentAt))
                Messages.Add(msg);
        }
        catch (Exception /*ex*/)
        {
            if (MauiApp.Current?.MainPage != null)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Error", "Failed to load chat messages", "OK");
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
                await _commandBus.SendAsync(new ChatCommands.UpdateChatMessageCommand(EditingMessage.LocalId, MessageText));
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
                var replyId = (ReplyToMessage?.LocalId > 0) ? ReplyToMessage!.LocalId : (int?)null;
                await _commandBus.SendAsync(new ChatCommands.SendChatMessageCommand("default_conversation", MessageText, "patient", DateTime.UtcNow, replyId));
                // optimistic UI message
                Messages.Add(new ChatMessage
                {
                    Content = MessageText,
                    SenderId = "patient",
                    ConversationId = "default_conversation",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Sent,
                    Type = MessageType.Text,
                    ReplyTo = ReplyToMessage,
                    ReplyToLocalId = replyId
                });
                MessageText = string.Empty;
                ReplyToMessage = null;
                await SimulateDoctorResponseAsync();
            }
            
            await ScrollToBottom();
        }
        catch (Exception /*ex*/)
        {
            if (MauiApp.Current?.MainPage != null)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Error", "Failed to send message", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task DeleteMessage(ChatMessage message)
    {
        if (message == null) return;
        try
        {
            await _commandBus.SendAsync(new ChatCommands.DeleteChatMessageCommand(message.LocalId));
            Messages.Remove(message);
        }
        catch (Exception /*ex*/)
        {
            if (MauiApp.Current?.MainPage != null)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Error", "Failed to delete message", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task HandleReplyToMessage(ChatMessage message)
    {
        _logger.LogDebug("HandleReplyToMessage invoked. Content={Content}, SenderId={SenderId}", message?.Content, message?.SenderId);
        
        if (message == null)
        {
            _logger.LogDebug("HandleReplyToMessage: message == null");
            return;
        }
        
        if (EditingMessage != null)
        {
            _logger.LogDebug("HandleReplyToMessage cancelled: EditingMessage != null");
            return;
        }
        
        if (message.SenderId == "patient")
        {
            _logger.LogDebug("HandleReplyToMessage cancelled: SenderId == patient");
            if (MauiApp.Current?.MainPage != null)
            {
                await MauiApp.Current.MainPage.DisplayAlert("Информация", "Нельзя ответить на собственное сообщение", "OK");
            }
            return;
        }
        
        try 
        {
            ReplyToMessage = message;
            _logger.LogDebug("ReplyToMessage set: {Content}", ReplyToMessage?.Content);
            
            await ScrollToBottom();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleReplyToMessage error");
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
        try
        {
            await Shell.Current.GoToAsync("//camera");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to camera failed");
        }
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        if (MauiApp.Current?.MainPage != null)
        {
            await MauiApp.Current.MainPage.DisplayAlert("Coming Soon", "Photo picker functionality will be available soon", "OK");
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
            
            if (MauiApp.Current?.MainPage != null)
            {
                await MauiApp.Current.MainPage.Dispatcher.DispatchAsync(() =>
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
            _logger.LogError(ex, "Error scrolling to bottom");
        }
    }

    private async Task SendPhotoMessageAsync(string localPath)
    {
        try
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("Preparing ChatMessage for image. LocalPath={Path}", localPath);

            var chatMessage = new ChatMessage
            {
                Content = string.Empty,
                SenderId = "patient",
                ConversationId = "default_conversation",
                SentAt = now,
                Timestamp = now,
                CreatedAt = now,
                Status = MessageStatus.Sent,
                Type = MessageType.Image,
                LocalAttachmentPath = localPath,
                SyncStatus = SyncStatus.NotSynced
            };

            _logger.LogInformation("Adding photo message to Messages collection. Thread={ThreadId}", Environment.CurrentManagedThreadId);

            if (MauiApp.Current?.Dispatcher.IsDispatchRequired ?? false)
            {
                await MauiApp.Current.Dispatcher.DispatchAsync(() => Messages.Add(chatMessage));
                _logger.LogInformation("Message added via Dispatcher. Total messages: {Count}", Messages.Count);
            }
            else
            {
                Messages.Add(chatMessage);
                _logger.LogInformation("Message added on current thread. Total messages: {Count}", Messages.Count);
            }

            await _commandBus.SendAsync(new ChatCommands.SendChatImageCommand("default_conversation", localPath, "patient", DateTime.UtcNow));

            await ScrollToBottom();

            _logger.LogInformation("SendPhotoMessageAsync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending photo message");
        }
    }
} 