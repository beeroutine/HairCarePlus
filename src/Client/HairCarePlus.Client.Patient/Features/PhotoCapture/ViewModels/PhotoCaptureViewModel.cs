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
using HairCarePlus.Shared.Communication;
using System.Text.Json;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Infrastructure.Storage;
using HairCarePlus.Client.Patient.Features.Sync.Application;
using HairCarePlus.Client.Patient.Infrastructure.Services.Interfaces;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;

public partial class PhotoCaptureViewModel : ObservableObject
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly ILogger<PhotoCaptureViewModel> _logger;
    private readonly HairCarePlus.Shared.Communication.IOutboxRepository _outboxRepo;
    private readonly IUploadService _uploadService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISyncService _syncService;
    private readonly IProfileService _profileService;
    private readonly CommunityToolkit.Mvvm.Messaging.IMessenger _messenger;

    public PhotoCaptureViewModel(ICommandBus commandBus,
                                 IQueryBus queryBus,
                                 ILogger<PhotoCaptureViewModel> logger,
                                 HairCarePlus.Shared.Communication.IOutboxRepository outboxRepo,
                                 IUploadService uploadService,
                                 IDbContextFactory<AppDbContext> dbFactory,
                                 ISyncService syncService,
                                 IProfileService profileService,
                                 CommunityToolkit.Mvvm.Messaging.IMessenger messenger)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
        _logger = logger;
        _outboxRepo = outboxRepo;
        _uploadService = uploadService;
        _dbFactory = dbFactory;
        _syncService = syncService;
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
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;

            // Open system camera UI
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo == null)
                return;

            // Save to cache
            var targetPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}.jpg");
            await using var stream = await photo.OpenReadAsync();
            await using var fs = File.OpenWrite(targetPath);
            await stream.CopyToAsync(fs);

            LastPhotoPath = targetPath;
            // Process captured photo (e.g., send to server)
            await HandleCapturedPhotoAsync(targetPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capture failed");
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось сделать снимок", "OK");
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

            // 1. Переносим файл в постоянное хранилище AppData/Media и немедленно уведомляем UI
            Guid reportId = Guid.NewGuid();
            DateTime capturedUtc = DateTime.UtcNow;

            // Гарантируем стабильный путь как в Instagram: AppData/Media/yyyyMMdd_HHmmss_{guid}.jpg
            var mediaDir = Path.Combine(FileSystem.AppDataDirectory, "Media");
            Directory.CreateDirectory(mediaDir);
            var persistentName = $"{capturedUtc:yyyyMMdd_HHmmss}_{reportId}.jpg";
            var persistentPath = Path.Combine(mediaDir, persistentName);
            var persisted = false;
            try
            {
                File.Copy(localPath, persistentPath, overwrite: true);
                persisted = File.Exists(persistentPath);
            }
            catch (Exception copyEx)
            {
                _logger.LogWarning(copyEx, "Initial copy to persistent storage failed. Retrying with stream copy...  src={Src} dst={Dst}", localPath, persistentPath);
            }

            if (!persisted)
            {
                try
                {
                    await using var src = File.OpenRead(localPath);
                    await using var dst = File.Open(persistentPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await src.CopyToAsync(dst);
                    persisted = File.Exists(persistentPath);
                }
                catch (Exception streamEx)
                {
                    _logger.LogError(streamEx, "Failed to persist captured photo to AppData/Media. src={Src} dst={Dst}", localPath, persistentPath);
                }
            }

            if (!persisted)
            {
                // Не записываем в БД путь из Cache, т.к. он может быть очищен при рестарте.
                // Без надежной локальной копии останавливаем обработку и попросим пользователя повторить снимок.
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось сохранить снимок в память устройства. Повторите попытку.", "OK");
                return;
            }

            await using (var db = await _dbFactory.CreateDbContextAsync())
            {
                // Persist relative file name in LocalPath so that future app sandbox moves don't break absolute paths
                var fileNameOnly = Path.GetFileName(persistentPath);
                db.PhotoReports.Add(new Features.Sync.Domain.Entities.PhotoReportEntity
                {
                    Id = reportId.ToString(),
                    ImageUrl = persistentPath,     // локальный постоянный путь; заменится после успешной загрузки
                    CaptureDate = capturedUtc,
                    DoctorComment = string.Empty,
                    // сохраняем относительный путь (имя файла) для устойчивости к изменению AppDataDirectory
                    LocalPath = fileNameOnly,
                    PatientId = _profileService.PatientId.ToString(),
                    Zone = PhotoZone.Front
                });
                await db.SaveChangesAsync();
            }

            // мгновенно обновляем ProgressPage корректным именем постоянного файла
            _messenger.Send(new PhotoSavedMessage(persistentName));

            // 2. Пытаемся загрузить файл (может занять время)
            var imageUrl = await _uploadService.UploadFileAsync(persistentPath, persistentName);

            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogWarning("Upload failed or offline. Will defer to next sync. Path={Path}", persistentPath);
                imageUrl = persistentPath; // SyncService позже загрузит файл и обновит запись.
            }

            // 3. Группируем снимки в сессию: один PhotoReportSetDto на три кадра
            // Сохраняем текущий кадр как элемент набора
            var currentItem = new HairCarePlus.Shared.Communication.PhotoReportItemDto
            {
                Type = HairCarePlus.Shared.Communication.PhotoType.Custom,
                ImageUrl = imageUrl,
                LocalPath = imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? null : persistentPath
            };

            // Временное хранилище набора в Preferences по стабильному ключу активной сессии,
            // чтобы три кадра гарантированно попали в один набор даже с паузами
            const string activeSetKey = "photo-set:active";
            HairCarePlus.Shared.Communication.PhotoReportSetDto set;
            if (Microsoft.Maui.Storage.Preferences.ContainsKey(activeSetKey))
            {
                var raw = Microsoft.Maui.Storage.Preferences.Get(activeSetKey, string.Empty);
                set = string.IsNullOrEmpty(raw)
                    ? new HairCarePlus.Shared.Communication.PhotoReportSetDto { Id = Guid.NewGuid(), PatientId = _profileService.PatientId, Date = capturedUtc }
                    : System.Text.Json.JsonSerializer.Deserialize<HairCarePlus.Shared.Communication.PhotoReportSetDto>(raw) ?? new HairCarePlus.Shared.Communication.PhotoReportSetDto { Id = Guid.NewGuid(), PatientId = _profileService.PatientId, Date = capturedUtc };
            }
            else
            {
                set = new HairCarePlus.Shared.Communication.PhotoReportSetDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = _profileService.PatientId,
                    Date = capturedUtc,
                    Notes = string.Empty
                };
            }
            set.Items.Add(currentItem);
            Microsoft.Maui.Storage.Preferences.Set(activeSetKey, System.Text.Json.JsonSerializer.Serialize(set));

            if (set.Items.Count >= 3)
            {
                // Набор готов – отправляем одним Outbox-элементом и очищаем кэш
                var outboxDto = new OutboxItemDto
                {
                    EntityType = nameof(HairCarePlus.Shared.Communication.PhotoReportSetDto),
                    Payload = JsonSerializer.Serialize(set),
                    LocalEntityId = set.Id.ToString(),
                    ModifiedAtUtc = DateTime.UtcNow
                };
                await _outboxRepo.AddAsync(outboxDto);
                Microsoft.Maui.Storage.Preferences.Remove(activeSetKey);
                _logger.LogInformation("PhotoReportSet enqueued. Id={Id} items={Count}", set.Id, set.Items.Count);
            }

            // 4. Немедленный триггер синхронизации, чтобы не ждать планировщик
            try
            {
                await _syncService.SynchronizeAsync(CancellationToken.None);
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, "Immediate sync after photo capture failed; will retry via scheduler");
            }
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

            // Если все фото сделаны – переходим на страницу Progress
            if (Templates.All(t => t.IsCaptured))
            {
                global::Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync("//progress");
                });
                return;
            }

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