using System.Collections.Generic;
using System;

namespace HairCarePlus.Shared.Communication.Sync;

public sealed class BatchSyncResponseDto
{
    public long NewSyncVersion { get; set; }

    // Server side changes which client must apply.
    public Dictionary<string, List<object>> Changes { get; set; } = new();

    public IReadOnlyList<ChatMessageDto> ChatMessages { get; init; } = new List<ChatMessageDto>();
    public IReadOnlyList<PhotoCommentDto> PhotoComments { get; init; } = new List<PhotoCommentDto>();
    public IReadOnlyList<CalendarTaskDto> CalendarTasks { get; init; } = new List<CalendarTaskDto>();
    public IReadOnlyList<PhotoReportDto>? PhotoReports { get; init; }
    public IReadOnlyList<ProgressEntryDto>? ProgressEntries { get; init; }
    public IReadOnlyList<RestrictionDto>? Restrictions { get; init; }

    // Ids of reports server wants from client (client must send full DTOs in next batch)
    public IReadOnlyList<Guid>? NeedPhotoReports { get; init; }
} 