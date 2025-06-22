using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Client.Clinic.Features.Chat.Models;
using HairCarePlus.Client.Clinic.Infrastructure.Network.Chat;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Clinic.Features.Chat.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatHubConnection _hubConnection;
    private const string ConversationId = "default_conversation";
    private const string CurrentUserId = "doctor";
    private readonly ILogger<ChatViewModel> _logger;
    private static bool _subscribed;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    [ObservableProperty]
    private string? _messageText;

    [ObservableProperty]
    private ChatMessage? _replyToMessage;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    [ObservableProperty]
    private PeerInfo _peer = new("Пациент Иванов", true);

    public IAsyncRelayCommand SendCommand { get; }
    public IRelayCommand BackCommand { get; }
    public IRelayCommand HideKeyboardCommand { get; }
    public IRelayCommand<ChatMessage> EditMessageCommand { get; }
    public IRelayCommand<ChatMessage> DeleteMessageCommand { get; }
    public IRelayCommand<ChatMessage> HandleReplyToMessageCommand { get; }
    public IRelayCommand CancelReplyCommand { get; }

    public ChatViewModel(IChatHubConnection hubConnection, ILogger<ChatViewModel> logger)
    {
        _hubConnection = hubConnection;
        if (!_subscribed)
        {
            _hubConnection.MessageReceived += OnMessageReceived;
            _subscribed = true;
        }
        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        BackCommand = new RelayCommand(() => { /* TODO: navigate back */ });
        HideKeyboardCommand = new RelayCommand(() => { /* TODO: hide keyboard */ });
        EditMessageCommand = new RelayCommand<ChatMessage>(_ => { /* edit stub */ });
        DeleteMessageCommand = new RelayCommand<ChatMessage>(msg => Messages.Remove(msg));
        HandleReplyToMessageCommand = new RelayCommand<ChatMessage>(HandleReplyToMessage);
        CancelReplyCommand = new RelayCommand(() => ReplyToMessage = null);
        _logger = logger;
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(MessageText);

    private async Task SendAsync()
    {
        var text = MessageText?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var replySender = ReplyToMessage?.SenderId;
        var replyContent = ReplyToMessage?.Content;

        MessageText = string.Empty;
        SendCommand.NotifyCanExecuteChanged();
        try
        {
            await _hubConnection.SendMessageAsync(ConversationId, CurrentUserId, text, replySender, replyContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send via hub – offline mode");
            // message already added locally, so just ignore
        }

        ReplyToMessage = null;
    }

    private void OnMessageReceived(object? sender, ChatMessageReceivedEventArgs e)
    {
        if (e.ConversationId != ConversationId) return;
        var msg = new ChatMessage
        {
            SenderId = e.SenderId,
            Content = e.Content,
            SentAt = e.SentAt,
            ReplyTo = e.ReplyToSenderId is null ? null : new ChatMessage
            {
                SenderId = e.ReplyToSenderId,
                Content = e.ReplyToContent ?? string.Empty
            }
        };
        App.Current?.Dispatcher.Dispatch(() => Messages.Add(msg));
    }

    public async Task InitializeAsync()
    {
        if (IsConnected) return;
        try
        {
            await _hubConnection.ConnectAsync();
            IsConnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat hub unavailable – running offline mode");
            IsConnected = false;
        }
    }

    partial void OnMessageTextChanged(string? oldValue, string? newValue)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    private void HandleReplyToMessage(ChatMessage? message)
    {
        if (message is null) return;
        if (message.SenderId == CurrentUserId) return; // don't reply to own message
        ReplyToMessage = message;
    }
}

public record PeerInfo(string Name, bool IsOnline); 