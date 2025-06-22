using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Server.API.Controllers;

[AllowAnonymous] // TODO: replace with [Authorize] when auth ready
public sealed class ChatHub : Hub<IChatClient>
{
    public async Task SendMessage(string conversationId, string senderId, string content, string isoUtc, string? replyToSenderId = null, string? replyToContent = null)
    {
        var dto = new ChatMessageDto
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTimeOffset.Parse(isoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
            ReplyToSenderId = replyToSenderId,
            ReplyToContent = replyToContent
        };
        // broadcast to conversation group
        await Clients.Group(conversationId).ReceiveMessage(dto);
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var conversationId = httpContext?.Request.Query["conversationId"].ToString();
        if (!string.IsNullOrEmpty(conversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
        await base.OnConnectedAsync();
    }
}

public interface IChatClient
{
    Task ReceiveMessage(ChatMessageDto dto);
} 