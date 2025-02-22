namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public class KeyboardEventArgs : EventArgs
    {
        public double KeyboardHeight { get; set; }

        public KeyboardEventArgs(double keyboardHeight)
        {
            KeyboardHeight = keyboardHeight;
        }
    }
} 