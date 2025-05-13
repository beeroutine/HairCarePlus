using CommunityToolkit.Maui.Views;

namespace HairCarePlus.Client.Patient.Features.Progress.Views;

public partial class ProcedureChecklistPopup : Popup
{
    public ProcedureChecklistPopup(object bindingContext)
    {
        InitializeComponent();
        BindingContext = bindingContext;
    }
} 