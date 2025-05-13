using System;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Вывод AI-сервиса по итогам анализа фото за выбранную дату.
/// </summary>
/// <param name="Date">Дата, к которой относится отчёт.</param>
/// <param name="Score">Сводная оценка 0-100 (чем выше — тем лучше).</param>
/// <param name="Summary">Markdown-описание рекомендаций/замечаний.</param>
public sealed record AIReport(DateOnly Date, int Score, string Summary); 