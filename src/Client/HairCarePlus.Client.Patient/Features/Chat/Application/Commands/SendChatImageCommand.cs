using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Commands;

public sealed record SendChatImageCommand(string ConversationId, string LocalImagePath, string SenderId, DateTime Timestamp) : ICommand;

public sealed class SendChatImageHandler : ICommandHandler<SendChatImageCommand>
{
    private readonly IChatRepository _repo;

    public SendChatImageHandler(IChatRepository repo) => _repo = repo;

    public async Task HandleAsync(SendChatImageCommand command, CancellationToken cancellationToken = default)
    {
        var message = new ChatMessage
        {
            ConversationId = command.ConversationId,
            Content = string.Empty,
            SenderId = command.SenderId,
            SentAt = command.Timestamp,
            Timestamp = command.Timestamp,
            Type = MessageType.Image,
            Status = MessageStatus.Sending,
            SyncStatus = SyncStatus.NotSynced,
            LocalAttachmentPath = command.LocalImagePath,
            FileName = System.IO.Path.GetFileName(command.LocalImagePath),
            MimeType = "image/jpeg"
        };

        await _repo.SaveMessageAsync(message, cancellationToken);

        message.Status = MessageStatus.Sent;
        await _repo.UpdateMessageAsync(message, cancellationToken);
    }
} 