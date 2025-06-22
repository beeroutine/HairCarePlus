using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Network.Chat;

public interface IChatHubConnection
{
    Task ConnectAsync();

    Task SendMessageAsync(string conversationId, string senderId, string content, string? replyToSenderId = null, string? replyToContent = null);

    event EventHandler<ChatMessageReceivedEventArgs>? MessageReceived;
}

public sealed class ChatMessageReceivedEventArgs : EventArgs
{
    public required string ConversationId { get; init; }
    public required string SenderId { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset SentAt { get; init; }
    public string? ReplyToSenderId { get; init; }
    public string? ReplyToContent { get; init; }
} 