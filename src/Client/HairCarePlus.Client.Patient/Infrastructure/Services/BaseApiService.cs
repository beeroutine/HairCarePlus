using System;
using System.Threading.Tasks;
using System.Text.Json;
using HairCarePlus.Client.Patient.Infrastructure.Storage;

namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public abstract class BaseApiService
    {
        protected readonly INetworkService NetworkService;
        protected readonly ILocalStorageService LocalStorageService;
        private readonly JsonSerializerOptions _jsonOptions;

        protected BaseApiService(
            INetworkService networkService,
            ILocalStorageService localStorageService)
        {
            NetworkService = networkService;
            LocalStorageService = localStorageService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        protected virtual async Task<T> ExecuteApiCallAsync<T>(Func<Task<T>> apiCall)
        {
            try
            {
                if (!await NetworkService.IsConnectedAsync())
                {
                    throw new Exception("No internet connection available");
                }

                return await apiCall();
            }
            catch (Exception ex)
            {
#if DEBUG
                // Log the error
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
#endif
                throw;
            }
        }

        protected virtual async Task<T> ExecuteWithCacheAsync<T>(
            string cacheKey,
            Func<Task<T>> apiCall,
            TimeSpan? cacheExpiration = null) where T : class
        {
            try
            {
                // Try to get from cache first
                var cachedData = await LocalStorageService.GetItemAsync<CacheEntry<T>>(cacheKey);
                if (cachedData != null && !IsCacheExpired(cachedData, cacheExpiration))
                {
                    return cachedData.Data;
                }

                // If not in cache or expired, call API
                var result = await ExecuteApiCallAsync(apiCall);

                // Cache the result
                await LocalStorageService.SetItemAsync(cacheKey, new CacheEntry<T>
                {
                    Data = result,
                    Timestamp = DateTime.UtcNow
                });

                return result;
            }
            catch (Exception ex)
            {
                // If offline and we have cached data, return it regardless of expiration
                var cachedData = await LocalStorageService.GetItemAsync<CacheEntry<T>>(cacheKey);
                if (cachedData != null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Using expired cache due to error: {ex.Message}");
#endif
                    return cachedData.Data;
                }

                throw;
            }
        }

        private bool IsCacheExpired<T>(CacheEntry<T> cacheEntry, TimeSpan? cacheExpiration)
        {
            if (!cacheExpiration.HasValue)
                return false;

            return DateTime.UtcNow - cacheEntry.Timestamp > cacheExpiration.Value;
        }
    }

    internal class CacheEntry<T>
    {
        public T Data { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
} 