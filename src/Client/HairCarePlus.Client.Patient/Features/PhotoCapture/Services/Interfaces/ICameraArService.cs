using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Services.Interfaces;

/// <summary>
/// Абстракция над камерой устройства с поддержкой AR-оверлеев.
/// Платформенные реализации размещаются посредством partial-классов.
/// </summary>
public interface ICameraArService
{
    /// <summary>
    /// Возвращает <c>true</c>, если устройство поддерживает полноценные AR-шаблоны (ARKit / ARCore).
    /// </summary>
    bool SupportsAr { get; }

    /// <summary>
    /// Запуск предварительного просмотра камеры. Вызывается из OnAppearing страницы.
    /// </summary>
    Task StartPreviewAsync();

    /// <summary>
    /// Остановка предварительного просмотра.
    /// </summary>
    Task StopPreviewAsync();

    /// <summary>
    /// Делает снимок и возвращает байты JPEG-изображения.
    /// </summary>
    Task<byte[]?> CaptureAsync();

    /// <summary>
    /// Переключение фронтальная/тыловая камера.
    /// </summary>
    Task SwitchCameraAsync();

    /// <summary>
    /// Включить/выключить вспышку.
    /// </summary>
    Task ToggleFlashAsync();
} 