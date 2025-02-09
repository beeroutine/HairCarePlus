using HairCarePlus.Client.Patient.Features.Profile.Models;
using HairCarePlus.Client.Patient.Infrastructure.Services;

namespace HairCarePlus.Client.Patient.Features.Profile.Services
{
    public class ProfileService : BaseApiService, IProfileService
    {
        private const string ProfileCacheKey = "patient_profile";
        private const string ApiEndpoint = "api/patient/profile";

        public ProfileService(
            INetworkService networkService,
            ILocalStorageService localStorageService)
            : base(networkService, localStorageService)
        {
        }

        public async Task<PatientProfile> GetProfileAsync()
        {
            return await ExecuteWithCacheAsync(
                ProfileCacheKey,
                async () => await NetworkService.GetAsync<PatientProfile>(ApiEndpoint),
                TimeSpan.FromMinutes(30));
        }

        public async Task<PatientProfile> UpdateProfileAsync(PatientProfile profile)
        {
            var updatedProfile = await ExecuteApiCallAsync(async () =>
                await NetworkService.PutAsync<PatientProfile>(ApiEndpoint, profile));

            // Update cache
            await LocalStorageService.SetAsync(ProfileCacheKey, new CacheEntry<PatientProfile>
            {
                Data = updatedProfile,
                Timestamp = DateTime.UtcNow
            });

            return updatedProfile;
        }

        public async Task<string> UploadProfilePhotoAsync(Stream photoStream, string fileName)
        {
            var response = await NetworkService.UploadFileAsync(
                $"{ApiEndpoint}/photo",
                photoStream,
                fileName,
                "image/jpeg");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result; // URL of the uploaded photo
            }

            throw new Exception("Failed to upload profile photo");
        }

        public async Task<bool> UpdatePreferredLanguageAsync(string languageCode)
        {
            var profile = await GetProfileAsync();
            profile.PreferredLanguage = languageCode;
            await UpdateProfileAsync(profile);
            return true;
        }

        public async Task<bool> UpdateTimeZoneAsync(string timeZoneId)
        {
            var profile = await GetProfileAsync();
            profile.TimeZoneId = timeZoneId;
            await UpdateProfileAsync(profile);
            return true;
        }

        public async Task<bool> AddMedicationAsync(string medication)
        {
            var profile = await GetProfileAsync();
            if (!profile.Medications.Contains(medication))
            {
                profile.Medications.Add(medication);
                await UpdateProfileAsync(profile);
            }
            return true;
        }

        public async Task<bool> RemoveMedicationAsync(string medication)
        {
            var profile = await GetProfileAsync();
            if (profile.Medications.Remove(medication))
            {
                await UpdateProfileAsync(profile);
            }
            return true;
        }

        public async Task<bool> AddAllergyAsync(string allergy)
        {
            var profile = await GetProfileAsync();
            if (!profile.Allergies.Contains(allergy))
            {
                profile.Allergies.Add(allergy);
                await UpdateProfileAsync(profile);
            }
            return true;
        }

        public async Task<bool> RemoveAllergyAsync(string allergy)
        {
            var profile = await GetProfileAsync();
            if (profile.Allergies.Remove(allergy))
            {
                await UpdateProfileAsync(profile);
            }
            return true;
        }

        public async Task<bool> SyncProfileAsync()
        {
            // Force refresh from server by bypassing cache
            await ExecuteApiCallAsync(async () =>
            {
                var profile = await NetworkService.GetAsync<PatientProfile>(ApiEndpoint);
                await LocalStorageService.SetAsync(ProfileCacheKey, new CacheEntry<PatientProfile>
                {
                    Data = profile,
                    Timestamp = DateTime.UtcNow
                });
                return true;
            });

            return true;
        }
    }
} 