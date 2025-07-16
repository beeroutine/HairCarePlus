using System;
using System.Collections.Generic;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Shared.Communication.Sync;
using HairCarePlus.Server.Domain.ValueObjects;

namespace HairCarePlus.Server.Application.Sync;

public static class SyncMappers
{
    public static PhotoComment ToEntity(this PhotoCommentDto dto)
    {
        return new PhotoComment(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.AuthorId,
            dto.PhotoReportId,
            dto.Text,
            dto.CreatedAtUtc
        );
    }

    public static PhotoCommentDto ToDto(this HairCarePlus.Server.Domain.ValueObjects.PhotoComment entity)
    {
        return new PhotoCommentDto
        {
            Id = entity.Id,
            PhotoReportId = entity.PhotoReportId,
            AuthorId = entity.AuthorId,
            Text = entity.Text,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static ChatMessage ToEntity(this ChatMessageDto dto)
    {
        var senderId = Guid.TryParse(dto.SenderId, out var s) ? s : Guid.Empty;
        var receiverId = Guid.TryParse(dto.ReceiverId ?? string.Empty, out var r) ? r : Guid.Empty;

        return new ChatMessage(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.Content,
            MessageType.Text, // Assuming text for now
            (HairCarePlus.Server.Domain.ValueObjects.MessageStatus)dto.Status,
            senderId,
            receiverId,
            dto.SentAt.UtcDateTime
        );
    }

    public static ChatMessageDto ToDto(this ChatMessage entity)
    {
        return new ChatMessageDto
        {
            Id = entity.Id,
            SenderId = entity.SenderId.ToString(),
            ReceiverId = entity.ReceiverId == Guid.Empty ? null : entity.ReceiverId.ToString(),
            Content = entity.Content,
            SentAt = entity.CreatedAt,
            Status = (HairCarePlus.Shared.Communication.MessageStatus)entity.Status
        };
    }

    public static TreatmentSchedule ToEntity(this CalendarTaskDto dto)
    {
        return new TreatmentSchedule(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.PatientId,
            dto.Title,
            dto.DueDateUtc,
            dto.IsDone,
            dto.IsSkipped
        );
    }

    public static CalendarTaskDto ToDto(this TreatmentSchedule entity)
    {
        return new CalendarTaskDto
        {
            Id = entity.Id,
            PatientId = Guid.Empty, // Not stored in entity
            Title = entity.Title,
            DueDateUtc = entity.StartDate,
            IsDone = entity.IsCompleted,
            IsSkipped = false // No direct mapping
        };
    }

    public static PhotoReport ToEntity(this PhotoReportDto dto)
    {
        return new PhotoReport(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.PatientId,
            dto.ImageUrl,
            null, // No upload URL when coming from DTO
            dto.ThumbnailUrl,
            dto.Date,
            dto.Notes,
            (HairCarePlus.Server.Domain.ValueObjects.PhotoType)dto.Type
        );
    }

    public static PhotoReportDto ToDto(this PhotoReport entity)
    {
        return new PhotoReportDto
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            ImageUrl = entity.ImageUploadUrl ?? entity.ImageUrl ?? string.Empty, // Prefer uploaded URL
            ThumbnailUrl = entity.ThumbnailUrl,
            Date = entity.CaptureDate,
            Notes = entity.Notes,
            Type = (HairCarePlus.Shared.Communication.PhotoType)entity.Type,
            Comments = new List<PhotoCommentDto>() // Simplified
        };
    }

    public static ProgressEntry ToEntity(this ProgressEntryDto dto)
    {
        return new ProgressEntry(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.PatientId,
            dto.DateUtc,
            dto.CompletedTasks,
            dto.TotalTasks
        );
    }

    public static ProgressEntryDto ToDto(this ProgressEntry entity)
    {
        return new ProgressEntryDto
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            DateUtc = entity.DateUtc,
            CompletedTasks = entity.CompletedTasks,
            TotalTasks = entity.TotalTasks
        };
    }

    public static Restriction ToEntity(this RestrictionDto dto)
    {
        return new Restriction(
            dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            dto.PatientId,
            (HairCarePlus.Server.Domain.ValueObjects.RestrictionType)dto.IconType,
            dto.StartUtc,
            dto.EndUtc,
            dto.IsActive
        );
    }

    public static RestrictionDto ToDto(this Restriction entity)
    {
        return new RestrictionDto
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            IconType = (HairCarePlus.Shared.Domain.Restrictions.RestrictionIconType)entity.Type,
            Type = 0, // obsolete, kept for backward compatibility
            StartUtc = entity.StartUtc,
            EndUtc = entity.EndUtc,
            IsActive = entity.IsActive
        };
    }

    // legacy Type property is unused by new clients
} 