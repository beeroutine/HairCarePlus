using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Maui.ApplicationModel;

namespace HairCarePlus.Client.Patient.Platforms.Android.Services
{
    public class KeyboardService : IKeyboardService
    {
        public void HideKeyboard()
        {
            var context = Platform.CurrentActivity;
            if (context == null) return;

            var inputMethodManager = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
            var token = context.CurrentFocus?.WindowToken;
            inputMethodManager?.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
        }
    }
} 