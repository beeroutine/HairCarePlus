using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;

[PrimaryKey("Id")]
public sealed class PhotoReportEntity
{
    public string Id { get; set; } = string.Empty; // Server-side Guid
    public string? PatientId { get; set; }
    public string? SetId { get; set; } // Guid of PhotoReportSet
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CaptureDate { get; set; }
    public string? DoctorComment { get; set; }
    public string? LocalPath { get; set; } // Absolute path of cached file, null until downloaded
    public PhotoZone Zone { get; set; }

    public List<PhotoCommentEntity> Comments { get; set; } = new();
} 