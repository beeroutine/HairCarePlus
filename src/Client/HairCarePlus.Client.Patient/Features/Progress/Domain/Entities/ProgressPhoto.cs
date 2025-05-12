using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

public enum PhotoZone
{
    Front,
    Top,
    Back
}

/// <summary>
/// Локально сохранённый снимок прогресса + результат AI-анализа.
/// </summary>
public sealed partial class ProgressPhoto : ObservableObject
{
    public required string LocalPath { get; init; }
    public required DateTime CapturedAt { get; init; }
    public required PhotoZone Zone { get; init; }

    /// <summary>
    /// Сводная оценка ИИ (0-100) – чем выше, тем лучше заживление.
    /// </summary>
    [ObservableProperty]
    private int _aiScore;

    /// <summary>
    /// Markdown-отчёт ИИ.
    /// </summary>
    [ObservableProperty]
    private string? _aiReport;
} 