using System;
using Microsoft.Maui.Controls; // For BindableObject if needed later
using HairCarePlus.Client.Patient.Features.Calendar.Models; // For EventType

namespace HairCarePlus.Client.Patient.Features.Calendar.Models
{
    /// <summary>
    /// Represents information about an active restriction for UI display.
    /// </summary>
    public class RestrictionInfo // Consider inheriting BindableObject if properties need dynamic updates
    {
        /// <summary>
        /// Material Icons glyph for the restriction type.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Number of days remaining until the restriction ends (ceiling value).
        /// </summary>
        public int RemainingDays { get; set; }

        /// <summary>
        /// Short description of the restriction (e.g., "Sport", "Water").
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The original EventType from the CalendarEvent.
        /// </summary>
        public EventType OriginalType { get; set; }

        /// <summary>
        /// The date the restriction ends.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Background color (circle fill) for the restriction badge.
        /// </summary>
        public Color BackgroundColor { get; set; } = Colors.LightGray;
    }
} 