using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;

[PrimaryKey("Id")]
public sealed class PhotoReportEntity
{
    public string Id { get; set; } = string.Empty; // Server-side Guid
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CaptureDate { get; set; }
    public string? DoctorComment { get; set; }

    public List<PhotoCommentEntity> Comments { get; set; } = new();
} 