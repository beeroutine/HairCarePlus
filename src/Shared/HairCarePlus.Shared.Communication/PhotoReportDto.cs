using System;
using System.Collections.Generic;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Shared.Communication;

public sealed class PhotoReportDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;
    public PhotoType Type { get; set; }
    public List<PhotoCommentDto> Comments { get; set; } = new();
    public string? LocalPath { get; set; }
}

public enum PhotoType
{
    FrontView,
    TopView,
    BackView,
    LeftSideView,
    RightSideView,
    Custom
} 