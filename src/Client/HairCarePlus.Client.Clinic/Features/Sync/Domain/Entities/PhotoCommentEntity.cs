using System;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

[PrimaryKey("Id")]
public sealed class PhotoCommentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhotoReportId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
} 