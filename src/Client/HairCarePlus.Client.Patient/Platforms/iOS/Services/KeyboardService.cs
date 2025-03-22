using Foundation;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using UIKit;
using System;

namespace HairCarePlus.Client.Patient.Platforms.iOS.Services
{
    public class KeyboardService : IKeyboardService
    {
        public event EventHandler<KeyboardEventArgs> KeyboardShown;
        public event EventHandler<KeyboardEventArgs> KeyboardHidden;

        private NSObject _keyboardShowObserver;
        private NSObject _keyboardHideObserver;
        private bool _isKeyboardVisible;

        public KeyboardService()
        {
            // Register for keyboard notifications using NSNotificationCenter instead of UIScene
            _keyboardShowObserver = UIKeyboard.Notifications.ObserveWillShow((sender, args) => {
                if (!_isKeyboardVisible)
                {
                    _isKeyboardVisible = true;
                    var keyboardFrame = UIKeyboard.FrameEndFromNotification(args.Notification);
                    KeyboardShown?.Invoke(this, new KeyboardEventArgs((float)keyboardFrame.Height));
                }
            });

            _keyboardHideObserver = UIKeyboard.Notifications.ObserveWillHide((sender, args) => {
                if (_isKeyboardVisible)
                {
                    _isKeyboardVisible = false;
                    KeyboardHidden?.Invoke(this, new KeyboardEventArgs(0));
                }
            });
        }

        public void HideKeyboard()
        {
            UIApplication.SharedApplication.KeyWindow?.EndEditing(true);
        }

        public void Dispose()
        {
            _keyboardShowObserver?.Dispose();
            _keyboardHideObserver?.Dispose();
        }
    }
} 