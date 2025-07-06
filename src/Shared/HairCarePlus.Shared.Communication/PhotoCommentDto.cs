using System;

namespace HairCarePlus.Shared.Communication;

public sealed class PhotoCommentDto
{
    public Guid Id { get; set; }
    public Guid PhotoReportId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
} 