using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.Application.Queries;

public sealed record GetChatMessagesQuery(string ConversationId) : IQuery<IReadOnlyList<ChatMessageDto>>;

public sealed class GetChatMessagesHandler : IQueryHandler<GetChatMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    private readonly IChatRepository _repo;
    public GetChatMessagesHandler(IChatRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ChatMessageDto>> HandleAsync(GetChatMessagesQuery query, CancellationToken cancellationToken = default)
        => (IReadOnlyList<ChatMessageDto>)await _repo.GetMessagesAsync(query.ConversationId, 0, 50, cancellationToken);
} 