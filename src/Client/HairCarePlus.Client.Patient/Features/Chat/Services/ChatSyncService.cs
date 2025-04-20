using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Repositories;
using HairCarePlus.Client.Patient.Infrastructure.Connectivity;

namespace HairCarePlus.Client.Patient.Features.Chat.Services;

public interface IChatSyncService
{
    Task SyncMessagesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasPendingSyncAsync(CancellationToken cancellationToken = default);
    Task HandleIncomingMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}

public class ChatSyncService : IChatSyncService
{
    private readonly IChatRepository _chatRepository;
    private readonly IConnectivityService _connectivityService;
    private readonly IChatApiClient _chatApiClient;
    private readonly ILogger<ChatSyncService> _logger;

    public ChatSyncService(
        IChatRepository chatRepository,
        IConnectivityService connectivityService,
        IChatApiClient chatApiClient,
        ILogger<ChatSyncService> logger)
    {
        _chatRepository = chatRepository;
        _connectivityService = connectivityService;
        _chatApiClient = chatApiClient;
        _logger = logger;
    }

    public async Task SyncMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connectivityService.IsConnected)
            {
                _logger.LogInformation("No internet connection available for sync");
                return;
            }

            var unsyncedMessages = await _chatRepository.GetUnsyncedMessagesAsync(cancellationToken);
            foreach (var message in unsyncedMessages)
            {
                try
                {
                    await _chatRepository.UpdateSyncStatusAsync(message.LocalId, SyncStatus.Syncing, cancellationToken: cancellationToken);

                    // Upload any attachments first
                    if (!string.IsNullOrEmpty(message.LocalAttachmentPath))
                    {
                        var attachmentUrl = await _chatApiClient.UploadAttachmentAsync(message.LocalAttachmentPath, cancellationToken);
                        message.AttachmentUrl = attachmentUrl;
                    }

                    if (!string.IsNullOrEmpty(message.LocalThumbnailPath))
                    {
                        var thumbnailUrl = await _chatApiClient.UploadAttachmentAsync(message.LocalThumbnailPath, cancellationToken);
                        message.ThumbnailUrl = thumbnailUrl;
                    }

                    // Send the message to server
                    var serverMessageId = await _chatApiClient.SendMessageAsync(message, cancellationToken);
                    
                    // Update local message with server ID and sync status
                    await _chatRepository.UpdateSyncStatusAsync(message.LocalId, SyncStatus.Synced, serverMessageId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync message {LocalId}", message.LocalId);
                    await _chatRepository.UpdateSyncStatusAsync(message.LocalId, SyncStatus.Failed, cancellationToken: cancellationToken);
                }
            }

            // Get any new messages from server
            var lastSyncTime = await GetLastSyncTimeAsync();
            var serverMessages = await _chatApiClient.GetMessagesAsync(lastSyncTime, cancellationToken);
            
            foreach (var serverMessage in serverMessages)
            {
                await HandleIncomingMessageAsync(serverMessage, cancellationToken);
            }

            await UpdateLastSyncTimeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during message synchronization");
            throw;
        }
    }

    public async Task<bool> HasPendingSyncAsync(CancellationToken cancellationToken = default)
    {
        var unsyncedMessages = await _chatRepository.GetUnsyncedMessagesAsync(cancellationToken);
        return unsyncedMessages.Any();
    }

    public async Task HandleIncomingMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we already have this message
            var existingMessage = await _chatRepository.GetMessageByServerIdAsync(message.ServerMessageId!, cancellationToken);
            if (existingMessage != null)
            {
                // Update existing message if needed
                existingMessage.Status = message.Status;
                existingMessage.IsRead = message.IsRead;
                existingMessage.ReadAt = message.ReadAt;
                existingMessage.DeliveredAt = message.DeliveredAt;
                await _chatRepository.UpdateMessageAsync(existingMessage, cancellationToken);
            }
            else
            {
                // Download attachments if present
                if (!string.IsNullOrEmpty(message.AttachmentUrl))
                {
                    var localPath = await _chatApiClient.DownloadAttachmentAsync(message.AttachmentUrl, cancellationToken);
                    message.LocalAttachmentPath = localPath;
                }

                if (!string.IsNullOrEmpty(message.ThumbnailUrl))
                {
                    var localPath = await _chatApiClient.DownloadAttachmentAsync(message.ThumbnailUrl, cancellationToken);
                    message.LocalThumbnailPath = localPath;
                }

                // Save new message
                message.SyncStatus = SyncStatus.Synced;
                await _chatRepository.SaveMessageAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling incoming message {MessageId}", message.ServerMessageId);
            throw;
        }
    }

    private async Task<DateTime> GetLastSyncTimeAsync()
    {
        // Implementation depends on where you want to store this
        // Could be in SecureStorage, Preferences, or your local database
        throw new NotImplementedException();
    }

    private async Task UpdateLastSyncTimeAsync()
    {
        // Implementation depends on where you want to store this
        throw new NotImplementedException();
    }
} 