using CommunityToolkit.Mvvm.ComponentModel;
using HairCarePlus.Shared.Domain.Restrictions;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Таймер ограничения (например, "Не пить алкоголь" ещё 10 дней).
/// </summary>
public sealed partial class RestrictionTimer : ObservableObject
{
    public required string Title { get; init; }

    /// <summary>
    /// Тип ограничения для выбора соответствующей SVG иконки.
    /// </summary>
    public required RestrictionIconType IconType { get; init; }

    /// <summary>
    /// Short label (1-2 words) shown under the circle.
    /// </summary>
    public string Label => Title;

    public string DetailedDescription { get; init; } = string.Empty;

    public double ProgressWidth => DaysRemaining <= 0 ? 300 : Math.Max(50, 300 - (DaysRemaining * 15));

    [ObservableProperty]
    private int _daysRemaining;

    public int TotalDays { get; init; }

    /// <summary>
    /// Normalised progress (0-1) from the operation/start date to the restriction cancellation date.
    /// Used by progress ring in UI.
    /// </summary>
    public double Progress => TotalDays <= 0 ? 1 : Math.Clamp((double)(TotalDays - DaysRemaining) / TotalDays, 0d, 1d);

    // Notify UI when DaysRemaining changes so that Progress is recalculated
    partial void OnDaysRemainingChanged(int oldValue, int newValue) => OnPropertyChanged(nameof(Progress));
} 