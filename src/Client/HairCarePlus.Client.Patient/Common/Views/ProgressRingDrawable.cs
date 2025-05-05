using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Common.Views
{
    /// <summary>
    /// Handles drawing the progress ring background and arc.
    /// </summary>
    public class ProgressRingDrawable : IDrawable
    {
        // Properties to be set by the hosting ProgressRingView
        public float Progress { get; set; } // 0.0f to 1.0f
        public Color TrackColor { get; set; } = Colors.LightGrey;
        public Color ProgressColor { get; set; } = Colors.Blue;
        public float StrokeWidth { get; set; } = 4f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;

            float effectiveSize = Math.Min(dirtyRect.Width, dirtyRect.Height);
            float radius = (effectiveSize - StrokeWidth) / 2f;
            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Center.Y;

            // Draw Track
            canvas.StrokeColor = TrackColor;
            canvas.StrokeSize = StrokeWidth;
            canvas.StrokeLineCap = LineCap.Round; // Make track ends round too
            canvas.DrawCircle(centerX, centerY, radius); // Draw full circle as track

            // Draw Progress Arc
            if (Progress > 0)
            {
                canvas.StrokeColor = ProgressColor;
                canvas.StrokeSize = StrokeWidth;
                canvas.StrokeLineCap = LineCap.Round; // Round ends for the progress arc

                // DrawArc expects start and end angles in degrees, clockwise from 3 o'clock.
                // We want to start at 12 o'clock (-90 degrees).
                float startAngle = -90;
                float endAngle = startAngle + (360 * Progress);

                // Define the bounding box for the arc
                float left = centerX - radius;
                float top = centerY - radius;
                float width = radius * 2;
                float height = radius * 2;
                
                // Draw the arc clockwise so filling direction matches clock hands
                canvas.DrawArc(left, top, width, height, startAngle, endAngle, true, false);
            }
        }
    }
} 