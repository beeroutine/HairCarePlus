using HairCarePlus.Client.Patient.Features.Profile.Models;

namespace HairCarePlus.Client.Patient.Features.Profile.Services
{
    public interface IProfileService
    {
        Task<PatientProfile> GetProfileAsync();
        Task<PatientProfile> UpdateProfileAsync(PatientProfile profile);
        Task<string> UploadProfilePhotoAsync(Stream photoStream, string fileName);
        Task<bool> UpdatePreferredLanguageAsync(string languageCode);
        Task<bool> UpdateTimeZoneAsync(string timeZoneId);
        Task<bool> AddMedicationAsync(string medication);
        Task<bool> RemoveMedicationAsync(string medication);
        Task<bool> AddAllergyAsync(string allergy);
        Task<bool> RemoveAllergyAsync(string allergy);
        Task<bool> SyncProfileAsync();
    }
} 