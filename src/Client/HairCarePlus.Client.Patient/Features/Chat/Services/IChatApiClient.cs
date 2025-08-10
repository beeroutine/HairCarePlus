using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Features.Chat.Services;

public interface IChatApiClient
{
    Task<string> SendMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<string> UploadAttachmentAsync(string localPath, CancellationToken cancellationToken = default);
    Task<string> DownloadAttachmentAsync(string url, CancellationToken cancellationToken = default);
} 