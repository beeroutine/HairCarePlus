using Microsoft.Maui.Controls;
using HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Maui.Views;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
// TODO: add CommunityToolkit.Maui.Camera integration when API finalized

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Views;

public partial class PhotoCapturePage : ContentPage
{
    private readonly PhotoCaptureViewModel _viewModel;
    private readonly ILogger<PhotoCapturePage> _logger;
    private readonly IMediaFileSystemService _fileSystem;
    private readonly IMessenger _messenger;
    private readonly HairCarePlus.Shared.Common.CQRS.ICommandBus _commandBus;
    private bool _previewReady;
    private bool _isCapturing;
    // Track cameras enumeration state
    private bool _camerasInitialized;
    private IReadOnlyList<object>? _availableCameras;

    // Placeholder for future camera lifecycle integration

    public PhotoCapturePage(PhotoCaptureViewModel viewModel,
                            ILogger<PhotoCapturePage> logger,
                            IMediaFileSystemService fileSystem,
                            IMessenger messenger,
                            HairCarePlus.Shared.Common.CQRS.ICommandBus commandBus)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        _fileSystem = fileSystem;
        _messenger = messenger;
        _commandBus = commandBus;
        BindingContext = _viewModel;

        // Observe ViewModel changes so we can switch cameras when user toggles the command
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PhotoCaptureViewModel.Facing))
        {
            _logger.LogInformation("Facing property changed, applying camera switch.");
            _ = SwitchFacingAsync();
        }
    }

    /// <summary>
    /// Switches between front and back camera using reflection-based fallback.
    /// </summary>
    private async Task SwitchFacingAsync()
    {
        try
        {
            // Ensure we have enumerated cameras at least once
            if (!_camerasInitialized)
            {
                try
                {
                    var cams = await Camera.GetAvailableCameras(CancellationToken.None);
                    _availableCameras = cams?.Cast<object>().ToList();
                    _camerasInitialized = _availableCameras?.Count > 0;
                    _logger.LogInformation($"Camera enumeration completed. Found {_availableCameras?.Count ?? 0} cameras.");
                }
                catch (Exception exEnum)
                {
                    _logger.LogError(exEnum, "Failed to enumerate cameras inside SwitchFacingAsync");
                }
            }

            var camerasList = _availableCameras;
            if (camerasList is null || camerasList.Count == 0)
            {
                _logger.LogWarning("No cameras available – cannot apply facing");
                return;
            }

            object? targetCamera = null;

            foreach (var cam in camerasList)
            {
                var positionProp = cam.GetType().GetProperty("Position");
                var positionVal = positionProp?.GetValue(cam)?.ToString()?.ToLowerInvariant();

                if (_viewModel.Facing == PhotoCaptureViewModel.CameraFacing.Front && positionVal?.Contains("front") == true)
                {
                    targetCamera = cam;
                    break;
                }
                if (_viewModel.Facing == PhotoCaptureViewModel.CameraFacing.Back && (positionVal?.Contains("back") == true || positionVal?.Contains("rear") == true))
                {
                    targetCamera = cam;
                    break;
                }
            }

            if (targetCamera is null)
            {
                _logger.LogWarning("Requested camera facing not available on this device.");
                return;
            }

            var selectedCameraProp = Camera.GetType().GetProperty("SelectedCamera");
            if (selectedCameraProp is null)
            {
                _logger.LogWarning("CameraView switch API not found – skipping.");
                return;
            }

            var current = selectedCameraProp.GetValue(Camera);
            if (!Equals(current, targetCamera))
            {
                selectedCameraProp.SetValue(Camera, targetCamera);
                _logger.LogInformation($"Switched camera to {_viewModel.Facing}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching camera generically via reflection.");
        }
    }

    private async Task<bool> TryStartPreviewAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting camera preview...");
            await Camera.StartCameraPreview(ct);
            _previewReady = true;
            _logger.LogInformation("Camera preview started successfully.");
            return true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("StartCameraPreview was cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start camera preview.");
            return false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Camera.MediaCaptured += OnMediaCaptured;

        // Ensure facing is correct after appearing
        await SwitchFacingAsync();

        // Always attempt to (re)start preview on appearing to avoid frozen frame after navigation.
        _logger.LogInformation("Attempting to start camera preview on appearing.");
            await TryStartPreviewAsync(CancellationToken.None);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Camera.MediaCaptured -= OnMediaCaptured;

        try
        {
            Camera.StopCameraPreview();
            _previewReady = false; // ensure preview restarts next time
            _logger.LogInformation("Camera preview stopped on disappearing.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop camera preview on disappearing");
        }

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnZoneChecked(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value != true) return;
        if (sender is RadioButton rb && rb.Content is string label)
        {
            var id = label switch
            {
                "Фронт" => "front",
                "Темя" => "top",
                "Затылок" => "back",
                _ => null
            };
            if (id != null)
            {
                _viewModel.SelectTemplateCommand.Execute(id);
            }
        }
    }

    private async void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
    {
        if (e?.Media == null)
        {
            _logger.LogWarning("MediaCaptured event without media stream.");
            return;
        }

        _isCapturing = false;
        byte[] bytes;
        using (var ms = new System.IO.MemoryStream())
        {
            await e.Media.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        if (bytes.Length == 0)
        {
            _logger.LogWarning("MediaCaptured event with empty data.");
            return;
        }

        var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

        try
        {
            var cacheDir = await _fileSystem.GetCacheDirectoryAsync();
            var targetSpecificDirectory = Path.Combine(cacheDir, "captured_photos");
            
            // Ensure the specific target directory exists
            if (!Directory.Exists(targetSpecificDirectory))
            {
                Directory.CreateDirectory(targetSpecificDirectory);
                _logger.LogInformation($"Created directory: {targetSpecificDirectory}");
            }

            var localPath = await _fileSystem.SaveFileAsync(bytes, fileName, targetSpecificDirectory);

            if (!string.IsNullOrEmpty(localPath))
            {
                _logger.LogInformation($"Photo saved to {localPath}");
                _messenger.Send(new HairCarePlus.Client.Patient.Features.PhotoCapture.Application.Messages.PhotoCapturedMessage(localPath));
                _viewModel.LastPhotoPath = localPath;
                // Immediately upload & enqueue to outbox
                await _viewModel.HandleCapturedPhotoAsync(localPath);
                if (_viewModel.SelectedTemplate is not null)
                {
                    _viewModel.SelectedTemplate.IsCaptured = true;

                    // автоматически выбрать следующий шаблон, который ещё не снят
                    if (_viewModel.Templates is not null)
                    {
                        var nextTemplate = _viewModel.Templates.FirstOrDefault(t => !t.IsCaptured);
                        if (nextTemplate is not null)
                            _viewModel.SelectedTemplate = nextTemplate;
                    }

                    // Если все фотографии сделаны, переходим в Progress
                    if (_viewModel.Templates?.Count > 0 && _viewModel.Templates.All(t => t.IsCaptured))
                    {
                        try
                        {
                            await Shell.Current.GoToAsync("//progress");
                        }
                        catch (Exception navEx)
                        {
                            _logger.LogError(navEx, "Navigation to progress after all photos captured failed");
                        }
                    }
                }
            }
            else
            {
                _logger.LogError("Failed to save photo, SaveFileAsync returned null or empty.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during photo saving process in OnMediaCaptured.");
        }
    }

    private async void OnShutterTapped(object? sender, TappedEventArgs e)
    {
        if (_isCapturing)
        {
            _logger.LogWarning("Capture in progress – shutter tap ignored.");
            return;
        }

        if (!_previewReady)
        {
            _logger.LogWarning("Preview not ready – attempting to start preview before capture.");
            await TryStartPreviewAsync(CancellationToken.None);
        }

        _logger.LogInformation("Shutter tapped – starting capture.");

        try
        {
            if (Camera.GetType().GetProperty("SelectedCamera")?.GetValue(Camera) is null)
            {
                _logger.LogWarning("No SelectedCamera set – cannot capture.");
                return;
            }

            _isCapturing = true;
            await Camera.CaptureImage(CancellationToken.None);
            _logger.LogInformation("Camera.CaptureImage called. Waiting for MediaCaptured event.");
            _isCapturing = false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("CaptureImage was cancelled.");
            _isCapturing = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Camera.CaptureImage");
            _isCapturing = false;
        }
    }

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        var path = _viewModel.LastPhotoPath;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var popup = new PhotoPreviewPopup(path);
            await this.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show photo preview popup.");
        }
    }
} 