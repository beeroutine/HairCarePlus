using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public interface IVibrationService
    {
        void Vibrate(int milliseconds);
        void VibrationPattern(long[] pattern);
        void Cancel();
        bool HasVibrator { get; }
    }
} 