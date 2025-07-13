using System;

namespace HairCarePlus.Client.Clinic.Features.Patient.Models;

public enum PhotoZone
{
    Front,
    Top,
    Back
}

public sealed class ProgressPhoto
{
    public required string ImageUrl { get; init; }
    public required DateTime CapturedAt { get; init; }
    public required PhotoZone Zone { get; init; }
} 