using HairCarePlus.Client.Patient.Infrastructure.Services;
using UIKit;

namespace HairCarePlus.Client.Patient.Platforms.iOS.Services
{
    public class KeyboardService : IKeyboardService
    {
        public void HideKeyboard()
        {
            UIApplication.SharedApplication.KeyWindow?.EndEditing(true);
        }
    }
} 