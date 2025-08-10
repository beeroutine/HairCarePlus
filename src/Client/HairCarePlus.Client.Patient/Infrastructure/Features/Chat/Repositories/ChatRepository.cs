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
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Infrastructure.Features.Chat.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMediaFileSystemService _fileSystemService;

    public ChatRepository(IDbContextFactory<AppDbContext> dbFactory, IMediaFileSystemService fileSystemService)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Include(m => m.ReplyTo)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessageDto?> GetMessageByLocalIdAsync(int localId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .Include(m => m.ReplyTo)
            .FirstOrDefaultAsync(m => m.LocalId == localId, cancellationToken);
    }

    public async Task<ChatMessageDto?> GetMessageByServerIdAsync(string serverId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .Include(m => m.ReplyTo)
            .FirstOrDefaultAsync(m => m.ServerMessageId == serverId, cancellationToken);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetUnreadMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsRead)
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetUnsyncedMessagesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .Where(m => m.SyncStatus == SyncStatus.NotSynced || m.SyncStatus == SyncStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId && !m.IsRead, cancellationToken);
    }

    public async Task<int> SaveMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.ChatMessages.AddAsync(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return message.LocalId;
    }

    public async Task UpdateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        message.LastModifiedAt = DateTime.UtcNow;
        db.ChatMessages.Update(message);
        await db.SaveChangesAsync(cancellationToken);
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

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            db.ChatMessages.Remove(message);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var messages = await db.ChatMessages
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

        db.ChatMessages.RemoveRange(messages);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveMessagesAsync(IEnumerable<ChatMessageDto> messages, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.ChatMessages.AddRangeAsync(messages, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(string conversationId, DateTime upToTimestamp, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var messages = await db.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SentAt <= upToTimestamp)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.LastModifiedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSyncStatusAsync(int localId, SyncStatus status, string? serverId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var message = await db.ChatMessages
            .FirstOrDefaultAsync(m => m.LocalId == localId, cancellationToken);
        if (message != null)
        {
            message.SyncStatus = status;
            if (serverId != null)
            {
                message.ServerMessageId = serverId;
            }
            message.LastModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteOldMessagesAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var oldMessages = await db.ChatMessages
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

        db.ChatMessages.RemoveRange(oldMessages);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupLocalAttachmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var messages = await db.ChatMessages
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

        await db.SaveChangesAsync(cancellationToken);
    }
} 