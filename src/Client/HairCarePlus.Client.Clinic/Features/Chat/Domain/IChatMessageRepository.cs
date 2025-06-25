using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Chat.Models;

namespace HairCarePlus.Client.Clinic.Features.Chat.Domain;

public interface IChatMessageRepository : Infrastructure.Storage.Repositories.IBaseRepository<ChatMessage>
{
    Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int limit = 100);
} 