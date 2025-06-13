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
using System.Reflection;
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
    private bool _camerasInitialized;
    private IReadOnlyList<object>? _availableCameras;
    private bool _isCapturing;

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
            ApplyFacing();
        }
    }

    /// <summary>
    /// Applies the requested camera facing (front/back) by selecting the appropriate
    /// camera from <see cref="CameraView.AvailableCameras"/> and assigning it to
    /// <see cref="CameraView.SelectedCamera"/>. Runs a defensive try / catch because the
    /// underlying API surface is different between versions of CommunityToolkit.Maui.Camera.
    /// </summary>
    private void ApplyFacing()
    {
        try
        {
            // Reflection based guard against API changes – we only run if the expected
            // properties are present to avoid hard compile-time coupling.
            var cameraViewType = Camera.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var selectedCameraProp = cameraViewType.GetProperty("SelectedCamera", flags);

            if (selectedCameraProp is null)
            {
                _logger.LogWarning("CameraView switch API not found – skipping ApplyFacing.");
                return;
            }

            // Use cached list if we have it, otherwise try to query synchronously via reflection fallback
            var camerasEnum = _availableCameras;
            if (camerasEnum is null)
            {
                var availableProp = cameraViewType.GetProperty("AvailableCameras")?.GetValue(Camera) as System.Collections.IEnumerable;
                camerasEnum = availableProp?.Cast<object>().ToList();
            }

            if (camerasEnum is null)
            {
                _logger.LogWarning("No camera list available – cannot apply facing yet.");
                return;
            }

            object? targetCamera = null;

            foreach (var cam in camerasEnum)
            {
                var positionProp = cam.GetType().GetProperty("Position");
                if (positionProp == null) continue;
                var positionVal = positionProp.GetValue(cam)?.ToString()?.ToLower();

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

            if (targetCamera != null)
            {
                var current = selectedCameraProp.GetValue(Camera);
                if (!Equals(current, targetCamera))
                {
                    selectedCameraProp.SetValue(Camera, targetCamera);
                    _logger.LogInformation($"Switched camera to {_viewModel.Facing}.");
                }
            }
            else
            {
                _logger.LogWarning("Requested camera facing not available on this device.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while switching camera.");
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

        if (!_camerasInitialized)
        {
            try
            {
                var cameras = await Camera.GetAvailableCameras(CancellationToken.None);
                _availableCameras = cameras?.Cast<object>().ToList();
                _camerasInitialized = _availableCameras?.Count > 0;
                _logger.LogInformation($"Camera enumeration completed. Found {_availableCameras?.Count ?? 0} cameras.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate cameras.");
            }
        }

        // Now apply facing (this will select the appropriate camera) and start preview
        ApplyFacing();

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