using System.Collections.Generic;

namespace HairCarePlus.Client.Clinic.Features.Patient.Models;

public sealed record ProgressFeedItem(
    DateOnly Date,
    string Title,
    string? Description,
    IReadOnlyList<ProgressPhoto> Photos,
    IReadOnlyList<string> ActiveRestrictions,
    string? DoctorReportSummary,
    AIReport? AiReport = null
); 