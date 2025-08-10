using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Infrastructure.Features.Chat.Repositories;

public class ChatMessageRepository : BaseRepository<ChatMessageDto>, IChatMessageRepository
{
    public ChatMessageRepository(IDbContextFactory<AppDbContext> dbFactory) : base(dbFactory)
    {
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessageHistoryAsync(int limit = 100, int offset = 0)
    {
        await using var db = await GetContextAsync();
        return await db.Set<ChatMessageDto>()
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessageDto>> GetUnreadMessagesAsync()
    {
        await using var db = await GetContextAsync();
        return await db.Set<ChatMessageDto>()
            .Where(m => m.Status == MessageStatus.Delivered && m.ReadAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid messageId)
    {
        await using var db = await GetContextAsync();
        var message = await db.Set<ChatMessageDto>().FindAsync(messageId);
        if (message != null)
        {
            message.ReadAt = DateTime.UtcNow;
            message.Status = MessageStatus.Read;
            message.LastModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAsDeliveredAsync(Guid messageId)
    {
        await using var db = await GetContextAsync();
        var message = await db.Set<ChatMessageDto>().FindAsync(messageId);
        if (message != null)
        {
            message.Status = MessageStatus.Delivered;
            message.LastModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateMessageStatusAsync(Guid messageId, MessageStatus status)
    {
        await using var db = await GetContextAsync();
        var message = await db.Set<ChatMessageDto>().FindAsync(messageId);
        if (message != null)
        {
            message.Status = status;
            message.LastModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
} 