namespace HairCarePlus.Client.Patient.Features.Progress.Services.Interfaces
{
    /// <summary>
    /// Provides drawing and interaction data for the annual timeline.
    /// </summary>
    public interface ITimelineService
    {
        /// <summary>
        /// Renders the timeline on the specified canvas.
        /// </summary>
        void DrawTimeline(Microsoft.Maui.Graphics.ICanvas canvas, Microsoft.Maui.Graphics.RectF dirtyRect, System.DateTime surgeryDate);

        /// <summary>
        /// Returns the marker message for the given surgery date.
        /// </summary>
        string GetMarkerMessage(System.DateTime surgeryDate, int totalDays = 365);
    }
} 