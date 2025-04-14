using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Chat.Services;

public interface IChatApiClient
{
    Task<string> SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<string> UploadAttachmentAsync(string localPath, CancellationToken cancellationToken = default);
    Task<string> DownloadAttachmentAsync(string url, CancellationToken cancellationToken = default);
} 