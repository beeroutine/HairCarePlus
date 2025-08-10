using System;
using System.Collections.Generic;

namespace HairCarePlus.Shared.Communication.Sync;

public sealed class BatchSyncRequestDto
{
    public string ClientId { get; set; } = string.Empty;
    public long LastSyncVersion { get; set; }

    public IReadOnlyList<ChatMessageDto> ChatMessages { get; init; } = new List<ChatMessageDto>();
    public IReadOnlyList<PhotoCommentDto> PhotoComments { get; init; } = new List<PhotoCommentDto>();
    public IReadOnlyList<CalendarTaskDto> CalendarTasks { get; init; } = new List<CalendarTaskDto>();

    // From patient side only
    public IReadOnlyList<PhotoReportDto>? PhotoReports { get; init; }
    public IReadOnlyList<ProgressEntryDto>? ProgressEntries { get; init; }
    public IReadOnlyList<RestrictionDto>? Restrictions { get; init; }

    // Lightweight diff for photo reports
    public IReadOnlyList<EntityHeaderDto>? PhotoReportHeaders { get; init; }

    public Guid DeviceId { get; init; }
    public Guid PatientId { get; init; }

    /// <summary>
    ///   Идентификаторы DeliveryQueue записей, которые клиент подтвердил (ACK)
    /// </summary>
    public IReadOnlyList<Guid>? AckIds { get; init; }
} 