using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Server.Infrastructure.Data.Repositories;
using HairCarePlus.Server.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Server.API.Controllers;

[AllowAnonymous] // TODO: replace with [Authorize] when auth ready
public sealed class ChatHub : Hub<IChatClient>
{
    private readonly IDeliveryQueueRepository _deliveryQueue;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IDeliveryQueueRepository deliveryQueue, ILogger<ChatHub> logger)
    {
        _deliveryQueue = deliveryQueue;
        _logger = logger;
    }

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

        // 1. Broadcast real0time for online clients
        await Clients.Group(conversationId).ReceiveMessage(dto);

        // 2. Enqueue for offline side (TTL 30 min)
        try
        {
            // Simple rule: if senderId starts with "clinic" -> receiverMask = 2 (patient), else 1 (clinic)
            byte receiverMask = senderId.StartsWith("clinic", StringComparison.OrdinalIgnoreCase) ? (byte)2 : (byte)1;
            var packet = new DeliveryQueue
            {
                EntityType = "ChatMessage",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(dto),
                PatientId = Guid.Empty, // Not linked to concrete patient yet
                ReceiversMask = receiverMask,
                DeliveredMask = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };
            await _deliveryQueue.AddRangeAsync(new[] { packet });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue chat message for offline delivery");
        }
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