using HairCarePlus.Client.Patient.Infrastructure.Services;
using Android.Views;
using Android.App;
using Microsoft.Maui.Platform;
using Android.Graphics;
using Android.Views.InputMethods;
using Android.Content;
using Microsoft.Maui.Controls;
using Application = Android.App.Application;

namespace HairCarePlus.Client.Patient.Platforms.Android.Services
{
    public class KeyboardService : IKeyboardService
    {
        public event EventHandler<KeyboardEventArgs> KeyboardShown;
        public event EventHandler<KeyboardEventArgs> KeyboardHidden;

        private readonly ViewTreeObserver _window;
        private bool _isKeyboardVisible;

        public KeyboardService()
        {
            var context = Application.Context;
            var activity = Platform.CurrentActivity;
            _window = activity.Window.DecorView.RootView.ViewTreeObserver;
            _window.GlobalLayout += OnGlobalLayout;
        }

        private void OnGlobalLayout(object sender, EventArgs e)
        {
            var rect = new global::Android.Graphics.Rect();
            Platform.CurrentActivity.Window.DecorView.GetWindowVisibleDisplayFrame(rect);
            var screenHeight = Platform.CurrentActivity.Window.DecorView.Height;
            var keyboardHeight = screenHeight - rect.Bottom;

            if (keyboardHeight > screenHeight * 0.15 && !_isKeyboardVisible)
            {
                _isKeyboardVisible = true;
                KeyboardShown?.Invoke(this, new KeyboardEventArgs(keyboardHeight));
            }
            else if (keyboardHeight < screenHeight * 0.15 && _isKeyboardVisible)
            {
                _isKeyboardVisible = false;
                KeyboardHidden?.Invoke(this, new KeyboardEventArgs(0));
            }
        }

        public void HideKeyboard()
        {
            var activity = Platform.CurrentActivity;
            var inputMethodManager = activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
            var token = activity.CurrentFocus?.WindowToken;
            inputMethodManager?.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
        }
    }
} 