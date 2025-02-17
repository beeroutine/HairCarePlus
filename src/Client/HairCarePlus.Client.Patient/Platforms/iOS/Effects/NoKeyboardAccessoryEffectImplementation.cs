using UIKit;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using HairCarePlus.Client.Patient.Effects;

namespace HairCarePlus.Client.Patient.Platforms.iOS.Effects;

public class NoKeyboardAccessoryEffectImplementation : PlatformEffect
{
    protected override void OnAttached()
    {
        if (Element == null || Control == null)
            return;

        if (Control is UITextField textField)
        {
            textField.InputAccessoryView = null;
        }
        else if (Control is UITextView textView)
        {
            textView.InputAccessoryView = null;
        }
    }

    protected override void OnDetached()
    {
    }
} 