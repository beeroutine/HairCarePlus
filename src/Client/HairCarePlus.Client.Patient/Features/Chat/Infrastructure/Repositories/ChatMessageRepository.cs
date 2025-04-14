using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Features.Chat.Infrastructure.Repositories;

public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(int limit = 100, int offset = 0)
    {
        return await DbSet
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync()
    {
        return await DbSet
            .Where(m => m.Status == MessageStatus.Delivered && m.ReadAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid messageId)
    {
        var message = await DbSet.FindAsync(messageId);
        if (message != null)
        {
            message.ReadAt = DateTime.UtcNow;
            message.Status = MessageStatus.Read;
            message.LastModifiedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }
    }

    public async Task MarkAsDeliveredAsync(Guid messageId)
    {
        var message = await DbSet.FindAsync(messageId);
        if (message != null)
        {
            message.Status = MessageStatus.Delivered;
            message.LastModifiedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }
    }

    public async Task UpdateMessageStatusAsync(Guid messageId, MessageStatus status)
    {
        var message = await DbSet.FindAsync(messageId);
        if (message != null)
        {
            message.Status = status;
            message.LastModifiedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }
    }
} 