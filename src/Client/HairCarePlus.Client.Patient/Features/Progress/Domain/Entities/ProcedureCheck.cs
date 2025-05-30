using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Отметка выполнения ежедневной процедуры ухода (мытьё, мазь и т.д.).
/// </summary>
public sealed partial class ProcedureCheck : ObservableObject
{
    /// <summary>
    /// Идентификатор или человекочитаемое название процедуры (например, "Wash", "Spray").
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Дата, к которой относится процедура (DateOnly вместо DateTime для чистоты домена).
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Выполнена ли процедура.
    /// </summary>
    [ObservableProperty]
    private bool _isDone;
} 