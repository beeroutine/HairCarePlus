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

    [ObservableProperty]
    private int _daysRemaining;
} 