using Android.Content;
using Android.OS;
using HairCarePlus.Client.Patient.Infrastructure.Services;
using Microsoft.Maui.Platform;
using Application = Android.App.Application;

namespace HairCarePlus.Client.Patient.Platforms.Android.Services
{
    public class VibrationService : IVibrationService
    {
        private readonly Vibrator _vibrator;
        
        public VibrationService()
        {
            _vibrator = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
        }

        public bool HasVibrator => _vibrator != null && _vibrator.HasVibrator;

        public void Vibrate(int milliseconds)
        {
            if (!HasVibrator) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                _vibrator.Vibrate(VibrationEffect.CreateOneShot(milliseconds, VibrationEffect.DefaultAmplitude));
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _vibrator.Vibrate(milliseconds);
#pragma warning restore CS0618
            }
        }

        public void VibrationPattern(long[] pattern)
        {
            if (!HasVibrator) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                _vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, -1));
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _vibrator.Vibrate(pattern, -1);
#pragma warning restore CS0618
            }
        }

        public void Cancel()
        {
            if (HasVibrator)
            {
                _vibrator.Cancel();
            }
        }
    }
} 