using CommunityToolkit.Maui.Views;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class DescriptionSheet : Popup
{
    public DescriptionSheet(string text)
    {
        InitializeComponent();
        BindingContext = text;
    }
} 