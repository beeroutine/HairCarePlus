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

                // DrawArc with sweepAngle for clockwise fill
                float startAngle = -90f; // 12 o'clock
                float sweepAngle = 360f * Progress; // calculate sweep based on progress

                // Define bounding box
                float left = centerX - radius;
                float top = centerY - radius;
                float width = radius * 2;
                float height = radius * 2;
                
                // Draw stroke-only arc clockwise
                canvas.DrawArc(left, top, width, height, startAngle, sweepAngle, false, true);
            }
        }
    }
} 