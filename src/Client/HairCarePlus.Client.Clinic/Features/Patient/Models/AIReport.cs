using System;

namespace HairCarePlus.Client.Clinic.Features.Patient.Models;

/// <summary>
/// AI analysis result for a set of progress photos.
/// Mirrors structure from Patient app so that shared XAML bindings compile.
/// </summary>
/// <param name="Date">Calendar date the report relates to.</param>
/// <param name="Score">Aggregated score 0-100 (higher is better recovery).</param>
/// <param name="Summary">Markdown summary of AI observations.</param>
public sealed record AIReport(DateOnly Date, int Score, string Summary); 