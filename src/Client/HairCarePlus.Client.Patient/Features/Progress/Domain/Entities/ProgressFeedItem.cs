using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

/// <summary>
/// Aggregated progress information for a single calendar day, ready to be displayed in the Progress feed.
/// </summary>
/// <param name="Date">The calendar day represented by the feed item.</param>
/// <param name="Photos">Collection of photos captured on this date (0-3 expected).</param>
/// <param name="ActiveRestrictions">Restrictions that were still active on this date.</param>
/// <param name="AiReport">Optional AI report generated from photos of this date.</param>
public sealed record ProgressFeedItem(
    DateOnly Date,
    IReadOnlyList<ProgressPhoto> Photos,
    IReadOnlyList<RestrictionTimer> ActiveRestrictions,
    AIReport? AiReport); 