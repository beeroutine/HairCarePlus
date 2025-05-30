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
        /// Unicode glyph (from Material Symbols) representing this restriction.
        /// </summary>
        public string IconGlyph { get; set; }

#pragma warning disable CS0618 // keep for backward-compat during refactor
        [Obsolete("Use IconGlyph instead.")]
        public string Icon { get => IconGlyph; set => IconGlyph = value; }
#pragma warning restore CS0618

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