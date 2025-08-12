using System;
using System.Collections.Generic;

namespace HairCarePlus.Shared.Communication;

/// <summary>
///   A single photo-report composed of exactly three photos captured in one session.
///   Delivered as ONE packet to guarantee atomicity.
/// </summary>
public sealed class PhotoReportSetDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    ///   Exactly three items: typically FrontView, TopView, BackView.
    /// </summary>
    public List<PhotoReportItemDto> Items { get; set; } = new();
}

public sealed class PhotoReportItemDto
{
    public PhotoType Type { get; set; }

    /// <summary>
    ///   Absolute URL to the uploaded image (http/https). If not available yet on the client,
    ///   the client may include LocalPath and server won't accept the set until URLs are HTTP.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    ///   Optional absolute local file path on device; used only client-side for upload retries.
    /// </summary>
    public string? LocalPath { get; set; }
}


