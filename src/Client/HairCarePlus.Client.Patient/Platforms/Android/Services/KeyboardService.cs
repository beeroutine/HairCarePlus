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
        public event EventHandler<KeyboardEventArgs>? KeyboardShown;
        public event EventHandler<KeyboardEventArgs>? KeyboardHidden;

        private ViewTreeObserver? _window;
        private bool _isKeyboardVisible;

        public KeyboardService()
        {
            var activity = Platform.CurrentActivity;
            var rootView = activity?.Window?.DecorView?.RootView;
            _window = rootView?.ViewTreeObserver;
            if (_window is not null)
            {
                _window.GlobalLayout += OnGlobalLayout;
            }
        }

        private void OnGlobalLayout(object sender, EventArgs e)
        {
            var activity = Platform.CurrentActivity;
            var decorView = activity?.Window?.DecorView;
            if (decorView is null)
            {
                return;
            }
            var rect = new global::Android.Graphics.Rect();
            decorView.GetWindowVisibleDisplayFrame(rect);
            var screenHeight = decorView.Height;
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
            if (activity is null)
            {
                return;
            }
            var inputMethodManager = activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
            var token = activity.CurrentFocus?.WindowToken;
            inputMethodManager?.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
        }
    }
} 