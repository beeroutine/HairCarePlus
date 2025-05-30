using CommunityToolkit.Maui.Views;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class InsightsSheet : Popup
{
    public InsightsSheet(object context)
    {
        InitializeComponent();
        BindingContext = context;
    }
} 