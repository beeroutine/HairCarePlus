using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Commands;

/// <summary>
/// Updates the content of an existing local chat message.
/// </summary>
/// <param name="LocalId">Local identifier of the message.</param>
/// <param name="Content">New content for the message.</param>
public sealed record UpdateChatMessageCommand(int LocalId, string Content) : ICommand;

public sealed class UpdateChatMessageHandler : ICommandHandler<UpdateChatMessageCommand>
{
    private readonly IChatRepository _repo;

    public UpdateChatMessageHandler(IChatRepository repo) => _repo = repo;

    public async Task HandleAsync(UpdateChatMessageCommand command, CancellationToken cancellationToken = default)
    {
        var message = await _repo.GetMessageByLocalIdAsync(command.LocalId, cancellationToken);
        if (message is null) return;

        message.Content = command.Content;
        message.LastModifiedAt = DateTime.UtcNow;
        message.Status = MessageStatus.Sent; // mark as sent optimistically

        await _repo.UpdateMessageAsync(message, cancellationToken);
    }
} 