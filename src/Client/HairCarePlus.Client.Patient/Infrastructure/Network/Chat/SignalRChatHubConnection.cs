using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Network.Chat;

public sealed class SignalRChatHubConnection : IChatHubConnection, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRChatHubConnection> _logger;

    public event EventHandler<ChatMessageReceivedEventArgs>? MessageReceived;

    public SignalRChatHubConnection(ILogger<SignalRChatHubConnection> logger)
    {
        _logger = logger;
        const string conversationId = "default_conversation";
        var baseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://192.168.1.6:5281";
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/chatHub?conversationId=" + conversationId)
            .WithAutomaticReconnect()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information))
            .Build();

        _connection.On<HairCarePlus.Shared.Communication.ChatMessageDto>(
            "ReceiveMessage",
            dto =>
            {
                MessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
                {
                    ConversationId = dto.ConversationId,
                    SenderId = dto.SenderId,
                    Content = dto.Content,
                    SentAt = dto.SentAt,
                    ReplyToSenderId = dto.ReplyToSenderId,
                    ReplyToContent = dto.ReplyToContent
                });
            });
    }

    public async Task ConnectAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
            _logger.LogInformation("Connected to chat hub (patient)");
        }
    }

    public async Task SendMessageAsync(string conversationId, string senderId, string content, string? replyToSenderId = null, string? replyToContent = null)
    {
        await ConnectAsync();
        var isoUtc = DateTimeOffset.UtcNow.ToString("o");
        await _connection.InvokeAsync("SendMessage", conversationId, senderId, content, isoUtc, replyToSenderId, replyToContent);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
} 