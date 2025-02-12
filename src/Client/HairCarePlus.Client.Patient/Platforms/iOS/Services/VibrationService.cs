using HairCarePlus.Client.Patient.Infrastructure.Services;
using UIKit;

namespace HairCarePlus.Client.Patient.Platforms.iOS.Services
{
    public class VibrationService : IVibrationService
    {
        public bool HasVibrator => true;

        public void Vibrate(int milliseconds)
        {
#pragma warning disable CA1422 // Type or member is obsolete
            UIImpactFeedbackGenerator feedback = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Medium);
            feedback.Prepare();
            feedback.ImpactOccurred();
#pragma warning restore CA1422
        }

        public void VibrationPattern(long[] pattern)
        {
            UINotificationFeedbackGenerator feedback = new UINotificationFeedbackGenerator();
            feedback.Prepare();
            feedback.NotificationOccurred(UINotificationFeedbackType.Success);
        }

        public void Cancel()
        {
            // iOS doesn't support canceling haptic feedback
        }
    }
} 