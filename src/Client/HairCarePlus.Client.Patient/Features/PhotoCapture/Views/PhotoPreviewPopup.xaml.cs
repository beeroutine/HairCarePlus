using CommunityToolkit.Maui.Views;

namespace HairCarePlus.Client.Patient.Features.PhotoCapture.Views;

public partial class PhotoPreviewPopup : Popup
{
    public string PhotoPath { get; }

    public PhotoPreviewPopup(string photoPath)
    {
        InitializeComponent();
        PhotoPath = photoPath;
        BindingContext = this;
    }
} 