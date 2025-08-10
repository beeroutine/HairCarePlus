using Android.Views.InputMethods;
using Microsoft.Maui.Platform;
using HairCarePlus.Client.Clinic.Infrastructure.Services;

namespace HairCarePlus.Client.Clinic.Platforms.Android.Services;

public class KeyboardService : IKeyboardService
{
    public void HideKeyboard()
    {
        var activity = Platform.CurrentActivity;
        var view = activity?.CurrentFocus;
        if (view != null)
        {
            var imm = activity?.GetSystemService(global::Android.Content.Context.InputMethodService) as InputMethodManager;
            imm?.HideSoftInputFromWindow(view.WindowToken, 0);
        }
    }
}