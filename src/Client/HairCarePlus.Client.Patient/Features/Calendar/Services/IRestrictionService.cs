using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Features.Calendar.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Calendar.Services;

/// <summary>
/// Сервис для работы с ограничениями после трансплантации
/// </summary>
public interface IRestrictionService
{
    /// <summary>
    /// Получает активные ограничения
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GetActiveRestrictionsAsync();

    /// <summary>
    /// Проверяет, есть ли активные ограничения на указанную дату
    /// </summary>
    Task<bool> HasActiveRestrictionsForDateAsync(DateTime date);

    /// <summary>
    /// Получает ограничения для указанного типа активности
    /// </summary>
    Task<IEnumerable<HairTransplantEvent>> GetRestrictionsForActivityTypeAsync(EventType eventType);

    /// <summary>
    /// Добавляет новое ограничение
    /// </summary>
    Task<HairTransplantEvent> AddRestrictionAsync(HairTransplantEvent restriction);

    /// <summary>
    /// Обновляет существующее ограничение
    /// </summary>
    Task<HairTransplantEvent> UpdateRestrictionAsync(HairTransplantEvent restriction);

    /// <summary>
    /// Удаляет ограничение
    /// </summary>
    Task<bool> RemoveRestrictionAsync(Guid id);
} 