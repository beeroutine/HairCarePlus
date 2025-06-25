using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Clinic.Features.Chat.Models;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;
using HairCarePlus.Client.Clinic.Infrastructure.Storage.Repositories;
using HairCarePlus.Client.Clinic.Features.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Chat.Repositories;

public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(AppDbContext ctx) : base(ctx) {}

    public async Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int limit = 100)
    {
        return await DbSet
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();
    }
} 