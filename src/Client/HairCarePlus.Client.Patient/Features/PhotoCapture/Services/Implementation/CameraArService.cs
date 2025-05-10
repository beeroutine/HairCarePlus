using System;
using System.IO;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Interfaces;
using Microsoft.Maui.Media;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Implementation;

/// <summary>
/// Кроссплатформенная реализация камеры без AR (fallback). Для устройств с AR
/// будут добавлены partial-классы <c>CameraArService.iOS</c> и <c>CameraArService.Android</c>.
/// </summary>
public class CameraArService : ICameraArService
{
    public bool SupportsAr => false; // Пока что AR не поддерживается

    public Task StartPreviewAsync()
    {
        // В fallback режиме ничего не делаем, так как MediaPicker открывает системный UI.
        return Task.CompletedTask;
    }

    public Task StopPreviewAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<byte[]?> CaptureAsync()
    {
        try
        {
            var result = await MediaPicker.CapturePhotoAsync();
            if (result == null)
                return null;

            await using var stream = await result.OpenReadAsync();
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task SwitchCameraAsync() => Task.CompletedTask; // Не поддерживается в MediaPicker

    public Task ToggleFlashAsync() => Task.CompletedTask;  // Не поддерживается в MediaPicker
} 