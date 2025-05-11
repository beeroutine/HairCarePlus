using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Domain.Entities;

/// <summary>
/// Данные AR-шаблона (оверлей, который помогает пользователю правильно позиционировать камеру).
/// </summary>
public sealed partial class CaptureTemplate : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>
    /// Путь или EmbeddedResource изображения/модели для наложения.
    /// </summary>
    public required string OverlayAsset { get; init; }
    /// <summary>
    /// Рекомендуемое расстояние до камеры в миллиметрах.
    /// </summary>
    public int? RecommendedDistanceMm { get; init; }
    /// <summary>
    /// Минимальный уровень освещённости (lux) для качественного кадра.
    /// </summary>
    public int? RecommendedLux { get; init; }

    [ObservableProperty]
    private bool _isCaptured;
} 