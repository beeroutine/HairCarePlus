using UIKit;
using HairCarePlus.Client.Clinic.Infrastructure.Services;

namespace HairCarePlus.Client.Clinic.Platforms.iOS.Services;

public class KeyboardService : IKeyboardService
{
    public void HideKeyboard()
    {
        UIApplication.SharedApplication.KeyWindow?.EndEditing(true);
    }
}