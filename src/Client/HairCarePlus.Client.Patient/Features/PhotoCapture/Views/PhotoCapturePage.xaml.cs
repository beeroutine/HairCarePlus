using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Views
{
public partial class PhotoCapturePage : ContentPage
{
    private readonly PhotoCaptureViewModel _viewModel;
    private readonly ILogger<PhotoCapturePage> _logger;
    private readonly IMediaFileSystemService _fileSystem;
    private readonly IMessenger _messenger;
        
        private readonly SemaphoreSlim _cameraLock = new(1, 1);
        private bool _isPreviewing;
    private bool _isCapturing;
    private IReadOnlyList<object>? _availableCameras;

        public PhotoCapturePage(
            PhotoCaptureViewModel viewModel,
                            ILogger<PhotoCapturePage> logger,
                            IMediaFileSystemService fileSystem,
            IMessenger messenger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        _fileSystem = fileSystem;
        _messenger = messenger;
        BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Camera.MediaCaptured += OnMediaCaptured;
            
            if (DeviceInfo.DeviceType == DeviceType.Virtual)
            {
                _logger.LogInformation("Virtual device detected, skipping camera setup.");
                return;
            }
            // Dispatch the async camera start to avoid blocking the UI thread and handle potential exceptions gracefully.
            Dispatcher.Dispatch(() => StartPreviewWithLockAsync());
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Camera.MediaCaptured -= OnMediaCaptured;
            
            await StopPreviewWithLockAsync();
        }

        private async Task StartPreviewWithLockAsync()
        {
            await _cameraLock.WaitAsync();
            try
            {
                if (_isPreviewing) return;

                await InitializeCamerasIfNeeded();
                
                var preferredCamera = FindCameraByFacing(_viewModel.Facing);
                if (preferredCamera != null)
                {
                    var selectedCameraProp = Camera.GetType().GetProperty("SelectedCamera");
                    selectedCameraProp?.SetValue(Camera, preferredCamera);
                }

                _logger.LogInformation("Starting camera preview...");
                await Camera.StartCameraPreview(CancellationToken.None);
                _isPreviewing = true;
                _logger.LogInformation("Camera preview started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start camera preview.");
            }
            finally
            {
                _cameraLock.Release();
            }
        }

        private async Task StopPreviewWithLockAsync()
        {
            await _cameraLock.WaitAsync();
            try
            {
                if (!_isPreviewing) return;
                
                _logger.LogInformation("Stopping camera preview.");
                Camera.StopCameraPreview();
                _isPreviewing = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop camera preview.");
            }
            finally
            {
                _cameraLock.Release();
            }
        }
        
        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhotoCaptureViewModel.Facing))
            {
                _logger.LogInformation("Facing property changed, restarting preview with new camera.");
                await StopPreviewWithLockAsync();
                await StartPreviewWithLockAsync();
            }
        }

        private async void OnShutterTapped(object? sender, TappedEventArgs e)
        {
            if (_isCapturing || !_isPreviewing)
            {
                _logger.LogWarning($"Shutter tapped but ignored. IsCapturing: {_isCapturing}, IsPreviewing: {_isPreviewing}");
            return;
        }

            await _cameraLock.WaitAsync();
            try
            {
                _isCapturing = true;
                await Camera.CaptureImage(CancellationToken.None);
                _logger.LogInformation("CaptureImage called. Waiting for MediaCaptured event.");
        }
        catch (Exception ex)
        {
                _logger.LogError(ex, "CaptureImage failed.");
                _isCapturing = false; // Release lock on error
            }
            finally
            {
                _cameraLock.Release();
        }
    }

    private async void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
        {
            try
    {
        if (e?.Media == null)
        {
                    _logger.LogWarning("MediaCaptured event received without media stream.");
            return;
        }

        byte[] bytes;
                using (var ms = new MemoryStream())
        {
            await e.Media.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        if (bytes.Length == 0)
        {
                    _logger.LogWarning("MediaCaptured event with empty data stream.");
            return;
        }

        var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var mediaDirectory = await _fileSystem.GetMediaDirectoryAsync();
                var localPath = await _fileSystem.SaveFileAsync(bytes, fileName, mediaDirectory);

                if (string.IsNullOrEmpty(localPath))
                {
                    _logger.LogError("Failed to save captured photo.");
                    return;
                }
                
                _logger.LogInformation($"Photo saved to {localPath}");
                _viewModel.LastPhotoPath = localPath;
                // Выполняем обработку отчёта асинхронно, не блокируя возможность следующего кадра
                _ = Task.Run(async () =>
                {
                    try
                    {
                await _viewModel.HandleCapturedPhotoAsync(localPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "HandleCapturedPhotoAsync failed in background.");
                    }
                });

                // Сразу разрешаем делать следующий снимок
                _isCapturing = false;

                if (_viewModel.SelectedTemplate != null)
                {
                    _viewModel.SelectedTemplate.IsCaptured = true;
                    var nextTemplate = _viewModel.Templates?.FirstOrDefault(t => !t.IsCaptured);
                    if (nextTemplate != null)
                    {
                            _viewModel.SelectedTemplate = nextTemplate;
                    }
                    else if (_viewModel.Templates?.All(t => t.IsCaptured) == true)
                        {
                            await Shell.Current.GoToAsync("//progress");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing captured media.");
            }
            // _isCapturing уже сброшен выше
        }
        
        private async Task InitializeCamerasIfNeeded()
        {
            if (_availableCameras != null) return;
            try
            {
                var cams = await Camera.GetAvailableCameras(CancellationToken.None);
                _availableCameras = cams?.Cast<object>().ToList();
                _logger.LogInformation($"Enumerated {_availableCameras?.Count ?? 0} cameras.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate available cameras.");
                _availableCameras = new List<object>();
            }
        }
        
        private object? FindCameraByFacing(PhotoCaptureViewModel.CameraFacing facing)
        {
            if (_availableCameras == null) return null;

            foreach (var cam in _availableCameras)
            {
                var positionProp = cam.GetType().GetProperty("Position");
                var positionVal = positionProp?.GetValue(cam)?.ToString()?.ToLowerInvariant();

                if (facing == PhotoCaptureViewModel.CameraFacing.Front && positionVal?.Contains("front") == true)
                    return cam;
                if (facing == PhotoCaptureViewModel.CameraFacing.Back && positionVal?.Contains("back") == true)
                    return cam;
            }
            return _availableCameras.FirstOrDefault();
        }
        
        private void OnZoneChecked(object? sender, CheckedChangedEventArgs e)
        {
            if (!e.Value || sender is not RadioButton rb || rb.Content is not string label) return;

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

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
            if (string.IsNullOrEmpty(_viewModel.LastPhotoPath)) return;

        try
        {
                await this.ShowPopupAsync(new PhotoPreviewPopup(_viewModel.LastPhotoPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show photo preview popup.");
            }
        }
    }
} 