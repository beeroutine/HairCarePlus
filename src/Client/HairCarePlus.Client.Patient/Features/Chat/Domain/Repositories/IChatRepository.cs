using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

public interface IChatRepository
{
    // Query methods
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<ChatMessage?> GetMessageByLocalIdAsync(int localId, CancellationToken cancellationToken = default);
    Task<ChatMessage?> GetMessageByServerIdAsync(string serverId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetUnsyncedMessagesAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default);
    
    // Command methods
    Task<int> SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task UpdateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(int localId, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    
    // Batch operations
    Task SaveMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string conversationId, DateTime upToTimestamp, CancellationToken cancellationToken = default);
    Task UpdateSyncStatusAsync(int localId, SyncStatus status, string? serverId = null, CancellationToken cancellationToken = default);
    
    // Cleanup
    Task DeleteOldMessagesAsync(DateTime before, CancellationToken cancellationToken = default);
    Task CleanupLocalAttachmentsAsync(CancellationToken cancellationToken = default);
} 