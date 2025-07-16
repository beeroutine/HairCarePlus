using System;

namespace HairCarePlus.Shared.Communication;

public sealed class PhotoReportCreateDto
{
    public string PatientId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CaptureDate { get; set; }
} 