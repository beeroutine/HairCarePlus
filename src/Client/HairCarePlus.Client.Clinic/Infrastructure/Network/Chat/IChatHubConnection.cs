using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Clinic.Infrastructure.Network.Chat;

public interface IChatHubConnection
{
    /// <summary>
    /// Connects to the SignalR chat hub and starts listening for messages.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Sends a message to the current conversation.
    /// </summary>
    /// <param name="conversationId">Conversation identifier.</param>
    /// <param name="senderId">Sender identifier.</param>
    /// <param name="content">Message body.</param>
    /// <param name="replyToSenderId">Optional reply sender identifier.</param>
    /// <param name="replyToContent">Optional reply content.</param>
    Task SendMessageAsync(string conversationId, string senderId, string content, string? replyToSenderId = null, string? replyToContent = null);

    /// <summary>
    /// Fires when a new message is received from the hub.
    /// </summary>
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