using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Таймер ограничения (например, "Не пить алкоголь" ещё 10 дней).
/// </summary>
public sealed partial class RestrictionTimer : ObservableObject
{
    public required string Title { get; init; }

    /// <summary>
    /// Short label (1-2 words) shown under the circle. For now returns <see cref="Title"/>; can be specialized later.
    /// </summary>
    public string Label => Title;

    /// <summary>
    /// Детальное описание ограничения для показа в Stories popup.
    /// </summary>
    public string DetailedDescription { get; init; } = string.Empty;

    /// <summary>
    /// Ширина прогресс бара для Stories popup (в процентах от максимальной ширины).
    /// Вычисляется как обратная пропорция от количества дней.
    /// </summary>
    public double ProgressWidth => DaysRemaining <= 0 ? 300 : Math.Max(50, 300 - (DaysRemaining * 15));

    [ObservableProperty]
    private int _daysRemaining;
} 