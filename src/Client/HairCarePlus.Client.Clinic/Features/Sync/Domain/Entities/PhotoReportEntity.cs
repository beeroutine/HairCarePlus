using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

[PrimaryKey("Id")]
public sealed class PhotoReportEntity
{
    public string Id { get; set; } = string.Empty; // Server-side Guid
    public string PatientId { get; set; } = string.Empty;
    public string? SetId { get; set; } // Guid of PhotoReportSet
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? DoctorComment { get; set; }
    public HairCarePlus.Shared.Communication.PhotoType Type { get; set; }
    public string? LocalPath { get; set; } // Absolute path of cached file, null until downloaded

    public List<PhotoCommentEntity> Comments { get; set; } = new();
} 