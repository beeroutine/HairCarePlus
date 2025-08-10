using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Shared.Communication;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Commands;

public sealed record SendChatMessageCommand(string ConversationId, string Content, string SenderId, DateTime Timestamp, int? ReplyToLocalId = null) : ICommand;

public sealed class SendChatMessageHandler : ICommandHandler<SendChatMessageCommand>
{
    private readonly IChatRepository _repo;
    private readonly IDbContextFactory<HairCarePlus.Client.Patient.Infrastructure.Storage.AppDbContext> _dbFactory;

    public SendChatMessageHandler(IChatRepository repo,
        IDbContextFactory<HairCarePlus.Client.Patient.Infrastructure.Storage.AppDbContext> dbFactory)
    {
        _repo = repo;
        _dbFactory = dbFactory;
    }

    public async Task HandleAsync(SendChatMessageCommand command, CancellationToken cancellationToken = default)
    {
        ChatMessageDto? replyTo = null;
        int? validReplyId = command.ReplyToLocalId.HasValue && command.ReplyToLocalId.Value > 0 ? command.ReplyToLocalId : null;
        if (validReplyId.HasValue)
        {
            replyTo = await _repo.GetMessageByLocalIdAsync(validReplyId.Value, cancellationToken);
        }

        var message = new ChatMessageDto
        {
            ConversationId = command.ConversationId,
            Content = command.Content,
            SenderId = command.SenderId,
            SentAt = command.Timestamp,
            Timestamp = command.Timestamp,
            Type = MessageType.Text,
            Status = MessageStatus.Sending,
            SyncStatus = SyncStatus.NotSynced,
            ReplyToLocalId = validReplyId,
            ReplyTo = replyTo
        };

        await _repo.SaveMessageAsync(message, cancellationToken);

        // Add to Outbox for sync
        var dto = new HairCarePlus.Shared.Communication.ChatMessageDto
        {
                        ConversationId = command.ConversationId,
            SenderId = command.SenderId,
            Content = command.Content,
            SentAt = command.Timestamp,
            Status = HairCarePlus.Shared.Communication.MessageStatus.Sent
        };

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.OutboxItems.AddAsync(new Features.Sync.Domain.Entities.OutboxItem
        {
            EntityType = "ChatMessage",
            Payload = System.Text.Json.JsonSerializer.Serialize(dto),
            CreatedAtUtc = DateTime.UtcNow,
            Status = HairCarePlus.Shared.Communication.OutboxStatus.Pending,
            LocalEntityId = message.LocalId.ToString()
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        // Optionally update status to Sent immediately (optimistic UI)
        message.Status = MessageStatus.Sent;
        await _repo.UpdateMessageAsync(message, cancellationToken);
    }
} 