using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands;

/// <summary>
/// Создать локальный PhotoReport и поместить его в Outbox для последующей синхронизации.
/// Пока реализовано как заглушка-no-op, чтобы убрать прямой вызов PhotoReportApi.
/// Полная реализация появится в задаче task5 (OutboxProcessor).
/// </summary>
public sealed record CreatePhotoReportCommand(byte[] ImageBytes, Guid PatientId, DateTime CapturedAtUtc) : ICommand;

internal sealed class CreatePhotoReportHandler : ICommandHandler<CreatePhotoReportCommand>
{
    private readonly ILogger<CreatePhotoReportHandler> _logger;
    public CreatePhotoReportHandler(ILogger<CreatePhotoReportHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(CreatePhotoReportCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub CreatePhotoReportHandler executed. Will be fully implemented later.");
        // TODO: save image to file system, insert PhotoReport entity + OutboxItem
        return Task.CompletedTask;
    }
} 