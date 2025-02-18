using HairCarePlus.Client.Patient.Common.Behaviors;

namespace HairCarePlus.Client.Patient.Infrastructure.Services;

public interface IKeyboardService
{
    event EventHandler<KeyboardEventArgs> KeyboardShown;
    event EventHandler<KeyboardEventArgs> KeyboardHidden;
    void HideKeyboard();
} 