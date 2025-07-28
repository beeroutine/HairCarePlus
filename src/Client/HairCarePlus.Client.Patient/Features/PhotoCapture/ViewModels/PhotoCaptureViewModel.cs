using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HairCarePlus.Shared.Common.CQRS;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Queries;
using System.Linq;
using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.Sync.Infrastructure;
using System.Text.Json;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;

public partial class PhotoCaptureViewModel : ObservableObject
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly ILogger<PhotoCaptureViewModel> _logger;
    private readonly IOutboxRepository _outbox;
    private readonly IUploadService _uploadService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IProfileService _profileService;
    private readonly CommunityToolkit.Mvvm.Messaging.IMessenger _messenger;

    public PhotoCaptureViewModel(ICommandBus commandBus,
                                 IQueryBus queryBus,
                                 ILogger<PhotoCaptureViewModel> logger,
                                 IOutboxRepository outbox,
                                 IUploadService uploadService,
                                 IDbContextFactory<AppDbContext> dbFactory,
                                 IProfileService profileService,
                                 CommunityToolkit.Mvvm.Messaging.IMessenger messenger)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
        _logger = logger;
        _outbox = outbox;
        _uploadService = uploadService;
        _dbFactory = dbFactory;
        _profileService = profileService;
        _messenger = messenger;

        _ = LoadTemplatesAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private IList<CaptureTemplate> _templates = new List<CaptureTemplate>();

    [ObservableProperty]
    private CaptureTemplate? _selectedTemplate;

    [ObservableProperty]
    private int _lux = 320;

    [ObservableProperty]
    private string? _lastPhotoPath;

    [ObservableProperty]
    private string? _instructionText;

    [ObservableProperty]
    private bool _showInstruction;

    public enum CameraFacing
    {
        Front,
        Back
    }

    [ObservableProperty]
    private CameraFacing _facing = CameraFacing.Front;

    [RelayCommand]
    private void ToggleFacing()
    {
        _logger.LogInformation($"ToggleFacingCommand called. Current facing: {_facing}");
        Facing = Facing == CameraFacing.Front ? CameraFacing.Back : CameraFacing.Front;
        _logger.LogInformation($"New facing: {_facing}");
        // TODO: publish message or invoke service to switch actual camera in view.
    }

    [RelayCommand]
    private async Task Capture()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _commandBus.SendAsync(new HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Commands.CapturePhotoCommand());

            if (!string.IsNullOrEmpty(_lastPhotoPath))
            {
                await HandleCapturedPhotoAsync(_lastPhotoPath);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Capture failed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Called by the view once a photo was physically saved to <paramref name="localPath"/>.
    /// Uploads the file, replaces ImageUrl with absolute URL and enqueues an Outbox item so that
    /// the next /sync/batch will deliver the report to the clinic.
    /// </summary>
    public async Task HandleCapturedPhotoAsync(string localPath)
    {
        if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
        {
            _logger.LogWarning("HandleCapturedPhotoAsync called with invalid path {Path}", localPath);
            return;
        }

        try
        {
            var fileName = Path.GetFileName(localPath);

            // 1. Сохраняем локально и немедленно уведомляем UI
            Guid reportId = Guid.NewGuid();
            DateTime capturedUtc = DateTime.UtcNow;

            await using (var db = await _dbFactory.CreateDbContextAsync())
            {
                db.PhotoReports.Add(new Features.Sync.Domain.Entities.PhotoReportEntity
                {
                    Id = reportId.ToString(),
                    ImageUrl = localPath,          // временно локальный путь; заменится после успешной загрузки
                    CaptureDate = capturedUtc,
                    DoctorComment = string.Empty,
                    // сохраняем полный путь, чтобы SyncService надёжно находил файл при повторном запуске
                    LocalPath = localPath,
                    PatientId = _profileService.PatientId.ToString(),
                    Zone = PhotoZone.Front
                });
                await db.SaveChangesAsync();
            }

            // мгновенно обновляем ProgressPage
            _messenger.Send(new PhotoSavedMessage(fileName));

            // 2. Пытаемся загрузить файл (может занять время)
            var imageUrl = await _uploadService.UploadFileAsync(localPath, fileName);

            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogWarning("Upload failed or offline. Will defer to next sync. Path={Path}", localPath);
                imageUrl = localPath; // SyncService позже загрузит файл и обновит запись.
            }

            // 3. Создаём DTO и Outbox (используем upload url или локальный путь)
            var dto = new HairCarePlus.Shared.Communication.PhotoReportDto
            {
                Id = reportId,
                PatientId = _profileService.PatientId, // предположим Guid
                ImageUrl = imageUrl,
                Date = capturedUtc,
                Notes = string.Empty,
                Type = HairCarePlus.Shared.Communication.PhotoType.Custom,
                Comments = new()
            };

            var item = new Features.Sync.Domain.Entities.OutboxItem
            {
                EntityType = "PhotoReport",
                PayloadJson = JsonSerializer.Serialize(dto),
                LocalEntityId = dto.Id.ToString(),
                ModifiedAtUtc = DateTime.UtcNow
            };

            await _outbox.AddAsync(item);

            _logger.LogInformation("PhotoReport enqueued. Id={Id} url={Url}", dto.Id, dto.ImageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle captured photo {Path}", localPath);
        }
    }

    [RelayCommand]
    private void SelectTemplate(string id)
    {
        if (Templates == null) return;
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == id);
    }

    partial void OnSelectedTemplateChanged(CaptureTemplate? oldValue, CaptureTemplate? newValue)
    {
        if (newValue != null)
        {
            InstructionText = $"Сфотографируйте {newValue.Name}";
            ShowInstruction = true;
        }
    }

    public void MarkCurrentAsCaptured()
    {
        if (SelectedTemplate != null)
        {
            SelectedTemplate.IsCaptured = true;
            ShowInstruction = false;
            // Wait briefly then move to next template and show again
            global::Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(600); // match fade time
                var currentIndex = Templates.IndexOf(SelectedTemplate);
                if (currentIndex + 1 < Templates.Count)
                {
                    SelectedTemplate = Templates[currentIndex + 1];
                }
            });
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("//today");
    }

    private async Task LoadTemplatesAsync()
    {
        try
        {
            var list = await _queryBus.SendAsync<IReadOnlyList<CaptureTemplate>>(new GetCaptureTemplatesQuery());
            Templates = new List<CaptureTemplate>(list);
            SelectedTemplate = Templates.FirstOrDefault();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error loading templates");
        }
    }
} 