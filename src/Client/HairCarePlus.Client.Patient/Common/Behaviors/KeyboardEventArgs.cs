namespace HairCarePlus.Client.Patient.Common.Behaviors;

public class KeyboardEventArgs : EventArgs
{
    public double KeyboardHeight { get; }

    public KeyboardEventArgs(double keyboardHeight)
    {
        KeyboardHeight = keyboardHeight;
    }
} 