using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using HairCarePlus.Client.Patient.Features.PhotoCapture.ViewModels;
using HairCarePlus.Client.Patient.Infrastructure.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls;
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
            _ = StartPreviewWithLockAsync();
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

                _logger.LogInformation("Starting camera preview...");

                await PickCameraForFacingAsync(_viewModel.Facing);

                await Camera.StartCameraPreview(CancellationToken.None);
                _isPreviewing = true;
                _logger.LogInformation($"Camera preview started successfully. Facing={_viewModel.Facing}");
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
                _logger.LogInformation("Facing changed, switching camera.");
                await SwitchCameraAsync();
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
                _logger.LogInformation("CaptureImage called.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaptureImage failed.");
                _isCapturing = false;
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
                    _logger.LogWarning("MediaCaptured event with no data.");
                    return;
                }
                using var ms = new MemoryStream();
                await e.Media.CopyToAsync(ms);
                var bytes = ms.ToArray();
                if (bytes.Length == 0) return;
                var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var dir = await _fileSystem.GetMediaDirectoryAsync();
                var path = await _fileSystem.SaveFileAsync(bytes, fileName, dir);
                _logger.LogInformation($"Saved to {path}");
                _viewModel.LastPhotoPath = path;
                _viewModel.MarkCurrentAsCaptured();
                _ = Task.Run(async () => await _viewModel.HandleCapturedPhotoAsync(path));
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private async Task PickCameraForFacingAsync(PhotoCaptureViewModel.CameraFacing facing, object? previousSelected = null)
        {
            var cameras = await Camera.GetAvailableCameras(CancellationToken.None);
            _availableCameras = cameras;

            if (cameras is null || cameras.Count == 0)
            {
                _logger.LogWarning("No cameras reported by CameraView.GetAvailableCameras().");
                return;
            }

            // Prefer strong match by position if available
            var preferred = cameras.FirstOrDefault(c =>
            {
                var pos = c.Position.ToString();
                var isFront = string.Equals(pos, "Front", StringComparison.OrdinalIgnoreCase);
                var isBackish = string.Equals(pos, "Back", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(pos, "Rear", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(pos, "BackCamera", StringComparison.OrdinalIgnoreCase);
                return facing == PhotoCaptureViewModel.CameraFacing.Front ? isFront : isBackish;
            });

            // Fallback: pick the other camera that isn't currently selected
            if (preferred is null && cameras.Count > 1)
            {
                var current = previousSelected ?? Camera.SelectedCamera;
                preferred = cameras.FirstOrDefault(c => !ReferenceEquals(c, current))
                            ?? cameras.First();
                _logger.LogInformation("Position match not found. Falling back to alternate camera.");
            }

            if (preferred is null)
            {
                preferred = cameras.First();
            }

            _logger.LogInformation($"Selecting camera: Position={preferred.Position}");
            if (_availableCameras is not null)
            {
                var list = string.Join(", ", _availableCameras.Select(c => c.ToString()));
                _logger.LogInformation($"Available cameras: {list}");
            }
            Camera.SelectedCamera = preferred;
        }

        private async Task SwitchCameraAsync()
        {
            await _cameraLock.WaitAsync();
            try
            {
                // Stop preview if running, then re-select camera and start preview again.
                object? previousSelected = Camera.SelectedCamera;
                if (_isPreviewing)
                {
                    _logger.LogInformation("Stopping preview to switch camera.");
                    Camera.StopCameraPreview();
                    _isPreviewing = false;
                }

                await PickCameraForFacingAsync(_viewModel.Facing, previousSelected);

                await Camera.StartCameraPreview(CancellationToken.None);
                _isPreviewing = true;
                _logger.LogInformation($"Camera switched. Now Facing={_viewModel.Facing}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch camera");
            }
            finally
            {
                _cameraLock.Release();
            }
        }
    }
} 