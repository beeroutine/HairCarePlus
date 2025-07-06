using System;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects;

/// <summary>
/// Comment left by clinic staff for a specific photo report.
/// </summary>
public class PhotoComment : BaseEntity
{
    public Guid AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    // EF navigation
    public Guid PhotoReportId { get; private set; }
    public PhotoReport PhotoReport { get; private set; }

    private PhotoComment() { }

    public PhotoComment(Guid photoReportId, Guid authorId, string text)
    {
        PhotoReportId = photoReportId;
        AuthorId = authorId;
        Text = text;
    }

    public PhotoComment(Guid id, Guid authorId, Guid photoReportId, string text, DateTimeOffset createdAtUtc)
    {
        Id = id;
        AuthorId = authorId;
        PhotoReportId = photoReportId;
        Text = text;
        CreatedAtUtc = createdAtUtc;
    }

    public void UpdateText(string newText)
    {
        if (!string.IsNullOrWhiteSpace(newText) && newText != Text)
            Text = newText;
    }
} 