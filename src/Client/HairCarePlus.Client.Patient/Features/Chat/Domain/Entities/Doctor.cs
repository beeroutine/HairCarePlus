using System;

namespace HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;

public class Doctor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Specialty { get; set; }
    public required string PhotoUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? Status { get; set; }
    public bool IsAvailableForChat { get; set; }
    public DateTime? NextAvailableTime { get; set; }
} 