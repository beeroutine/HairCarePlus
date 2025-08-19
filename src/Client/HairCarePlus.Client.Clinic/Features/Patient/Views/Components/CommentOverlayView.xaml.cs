namespace HairCarePlus.Client.Clinic.Features.Patient.Views.Components;

public partial class CommentOverlayView : ContentView
{
    public CommentOverlayView()
    {
        InitializeComponent();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsVisible) && IsVisible)
            {
                // Focus after layout is ready
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => CommentEditor?.Focus());
            }
        };
    }
}


