using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Queries;

public sealed record GetChatMessagesQuery(string ConversationId) : IQuery<IReadOnlyList<ChatMessage>>;

public sealed class GetChatMessagesHandler : IQueryHandler<GetChatMessagesQuery, IReadOnlyList<ChatMessage>>
{
    private readonly IChatRepository _repo;
    public GetChatMessagesHandler(IChatRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ChatMessage>> HandleAsync(GetChatMessagesQuery query, CancellationToken cancellationToken = default)
        => (IReadOnlyList<ChatMessage>)await _repo.GetMessagesAsync(query.ConversationId, 0, 50, cancellationToken);
} 