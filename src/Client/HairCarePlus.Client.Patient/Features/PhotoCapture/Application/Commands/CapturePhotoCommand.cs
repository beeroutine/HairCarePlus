using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Interfaces;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using Microsoft.Extensions.Logging;
using System.IO;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands;

/// <summary>
/// Команда захвата фото с сохранением в локальное хранилище.
/// </summary>
public sealed record CapturePhotoCommand : ICommand;

public sealed class CapturePhotoHandler : ICommandHandler<CapturePhotoCommand>
{
    private readonly ICameraArService _cameraService;
    private readonly IMediaFileSystemService _fileSystem;
    private readonly IMessenger _messenger;
    private readonly ILogger<CapturePhotoHandler> _logger;
    private readonly ICommandBus _commandBus;
    private readonly IProfileService _profileService;

    public CapturePhotoHandler(ICameraArService cameraService,
                               IMediaFileSystemService fileSystem,
                               IMessenger messenger,
                               ILogger<CapturePhotoHandler> logger,
                               ICommandBus commandBus,
                               IProfileService profileService)
    {
        _cameraService = cameraService;
        _fileSystem = fileSystem;
        _messenger = messenger;
        _logger = logger;
        _commandBus = commandBus;
        _profileService = profileService;
    }

    public async Task HandleAsync(CapturePhotoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _cameraService.CaptureAsync();
            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogWarning("CaptureAsync returned no data");
                return;
            }

            var mediaDir = await _fileSystem.GetMediaDirectoryAsync();
            var fileName = $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            var path = await _fileSystem.SaveFileAsync(bytes, fileName, mediaDir);

            _messenger.Send(new PhotoCapturedMessage(path));
            _logger.LogInformation("Photo captured and saved to {Path}", path);

            // Upload to server as data URI so Clinic can display immediately
            await _commandBus.SendAsync(new CreatePhotoReportCommand(bytes, _profileService.PatientId, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing photo");
            throw;
        }
    }
} 