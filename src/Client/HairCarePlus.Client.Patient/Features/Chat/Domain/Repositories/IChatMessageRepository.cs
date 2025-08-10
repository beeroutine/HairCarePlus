using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

public interface IChatMessageRepository : IBaseRepository<ChatMessageDto>
{
    Task<IEnumerable<ChatMessageDto>> GetMessageHistoryAsync(int limit = 100, int offset = 0);
    Task<IEnumerable<ChatMessageDto>> GetUnreadMessagesAsync();
    Task UpdateMessageStatusAsync(Guid messageId, MessageStatus status);
} 