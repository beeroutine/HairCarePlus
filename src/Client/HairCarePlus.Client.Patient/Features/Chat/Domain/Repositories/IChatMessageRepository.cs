using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

public interface IChatMessageRepository : IBaseRepository<ChatMessage>
{
    Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(int limit = 100, int offset = 0);
    Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync();
    Task MarkAsReadAsync(Guid messageId);
    Task MarkAsDeliveredAsync(Guid messageId);
    Task UpdateMessageStatusAsync(Guid messageId, MessageStatus status);
} 