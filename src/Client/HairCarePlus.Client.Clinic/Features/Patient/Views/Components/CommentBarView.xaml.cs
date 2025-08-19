using System;

namespace HairCarePlus.Client.Clinic.Features.Patient.Views.Components;

public partial class CommentBarView : ContentView
{
    public CommentBarView()
    {
        InitializeComponent();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsVisible) && IsVisible)
            {
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => CommentEntry.Focus());
            }
        };
    }
}


