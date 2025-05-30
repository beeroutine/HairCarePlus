using CommunityToolkit.Maui.Views;
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class RestrictionStoriesPopup : Popup
{
    public RestrictionStoriesPopup(RestrictionTimer restriction)
    {
        InitializeComponent();
        BindingContext = restriction;
    }

    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        Close();
    }
} 