using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Infrastructure.Features.Chat.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMediaFileSystemService _fileSystemService;

    public ChatRepository(AppDbContext dbContext, IMediaFileSystemService fileSystemService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Include(m => m.ReplyTo)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage?> GetMessageByLocalIdAsync(int localId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Include(m => m.ReplyTo)
            .FirstOrDefaultAsync(m => m.LocalId == localId, cancellationToken);
    }

    public async Task<ChatMessage?> GetMessageByServerIdAsync(string serverId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Include(m => m.ReplyTo)
            .FirstOrDefaultAsync(m => m.ServerMessageId == serverId, cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsRead)
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetUnsyncedMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.SyncStatus == SyncStatus.NotSynced || m.SyncStatus == SyncStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId && !m.IsRead, cancellationToken);
    }

    public async Task<int> SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message.LocalId;
    }

    public async Task UpdateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        message.LastModifiedAt = DateTime.UtcNow;
        _dbContext.ChatMessages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMessageAsync(int localId, CancellationToken cancellationToken = default)
    {
        var message = await GetMessageByLocalIdAsync(localId, cancellationToken);
        if (message != null)
        {
            if (!string.IsNullOrEmpty(message.LocalAttachmentPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalAttachmentPath);
            }
            if (!string.IsNullOrEmpty(message.LocalThumbnailPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalThumbnailPath);
            }

            _dbContext.ChatMessages.Remove(message);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (!string.IsNullOrEmpty(message.LocalAttachmentPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalAttachmentPath);
            }
            if (!string.IsNullOrEmpty(message.LocalThumbnailPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalThumbnailPath);
            }
        }

        _dbContext.ChatMessages.RemoveRange(messages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatMessages.AddRangeAsync(messages, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(string conversationId, DateTime upToTimestamp, CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SentAt <= upToTimestamp)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.LastModifiedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSyncStatusAsync(int localId, SyncStatus status, string? serverId = null, CancellationToken cancellationToken = default)
    {
        var message = await GetMessageByLocalIdAsync(localId, cancellationToken);
        if (message != null)
        {
            message.SyncStatus = status;
            if (serverId != null)
            {
                message.ServerMessageId = serverId;
            }
            message.LastModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteOldMessagesAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        var oldMessages = await _dbContext.ChatMessages
            .Where(m => m.SentAt < before && m.SyncStatus == SyncStatus.Synced)
            .ToListAsync(cancellationToken);

        foreach (var message in oldMessages)
        {
            if (!string.IsNullOrEmpty(message.LocalAttachmentPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalAttachmentPath);
            }
            if (!string.IsNullOrEmpty(message.LocalThumbnailPath))
            {
                await _fileSystemService.DeleteFileAsync(message.LocalThumbnailPath);
            }
        }

        _dbContext.ChatMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupLocalAttachmentsAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m => m.LocalAttachmentPath != null || m.LocalThumbnailPath != null)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (!string.IsNullOrEmpty(message.LocalAttachmentPath) && !await _fileSystemService.FileExistsAsync(message.LocalAttachmentPath))
            {
                message.LocalAttachmentPath = null;
            }
            if (!string.IsNullOrEmpty(message.LocalThumbnailPath) && !await _fileSystemService.FileExistsAsync(message.LocalThumbnailPath))
            {
                message.LocalThumbnailPath = null;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
} 