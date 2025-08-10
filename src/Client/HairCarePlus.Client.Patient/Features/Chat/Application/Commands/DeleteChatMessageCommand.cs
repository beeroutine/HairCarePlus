using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Commands;

/// <summary>
/// Removes a chat message from local storage.
/// </summary>
/// <param name="LocalId">Local identifier of the message to delete.</param>
public sealed record DeleteChatMessageCommand(int LocalId) : ICommand;

public sealed class DeleteChatMessageHandler : ICommandHandler<DeleteChatMessageCommand>
{
    private readonly IChatRepository _repo;
    public DeleteChatMessageHandler(IChatRepository repo) => _repo = repo;

    public async Task HandleAsync(DeleteChatMessageCommand command, CancellationToken cancellationToken = default)
        => await _repo.DeleteMessageAsync(command.LocalId, cancellationToken);
} 