using Foundation;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using UIKit;

namespace HairCarePlus.Client.Patient.Platforms.iOS.Services
{
    public class KeyboardService : IKeyboardService
    {
        public event EventHandler<KeyboardEventArgs> KeyboardShown;
        public event EventHandler<KeyboardEventArgs> KeyboardHidden;

        public KeyboardService()
        {
            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardShown);
            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardHidden);
        }

        private void OnKeyboardShown(NSNotification notification)
        {
            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
            KeyboardShown?.Invoke(this, new KeyboardEventArgs(keyboardFrame.Height));
        }

        private void OnKeyboardHidden(NSNotification notification)
        {
            KeyboardHidden?.Invoke(this, new KeyboardEventArgs(0));
        }

        public void HideKeyboard()
        {
            var scenes = UIApplication.SharedApplication.ConnectedScenes;
            var windowScene = scenes.ToArray()
                .FirstOrDefault(s => s is UIWindowScene) as UIWindowScene;
            
            if (windowScene != null)
            {
                var windows = windowScene.Windows;
                var keyWindow = windows.FirstOrDefault(w => w.IsKeyWindow);
                keyWindow?.EndEditing(true);
            }
        }
    }
} 