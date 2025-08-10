using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;

public interface IChatRepository
{
    // Query methods
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<ChatMessageDto?> GetMessageByLocalIdAsync(int localId, CancellationToken cancellationToken = default);
    Task<ChatMessageDto?> GetMessageByServerIdAsync(string serverId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageDto>> GetUnreadMessagesAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageDto>> GetUnsyncedMessagesAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default);
    
    // Command methods
    Task<int> SaveMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);
    Task UpdateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(int localId, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    
    // Batch operations
    Task SaveMessagesAsync(IEnumerable<ChatMessageDto> messages, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string conversationId, DateTime upToTimestamp, CancellationToken cancellationToken = default);
    Task UpdateSyncStatusAsync(int localId, SyncStatus status, string? serverId = null, CancellationToken cancellationToken = default);
    
    // Cleanup
    Task DeleteOldMessagesAsync(DateTime before, CancellationToken cancellationToken = default);
    Task CleanupLocalAttachmentsAsync(CancellationToken cancellationToken = default);
} 