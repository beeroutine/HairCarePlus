using System;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Security;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<bool> RemoveAsync(string key);
    Task RemoveAllAsync();
    Task<bool> ContainsKeyAsync(string key);
    
    // Specialized methods for sensitive data
    Task<string?> GetAuthTokenAsync();
    Task SetAuthTokenAsync(string token);
    Task<DateTime?> GetTokenExpirationAsync();
    Task SetTokenExpirationAsync(DateTime expiration);
    Task ClearAuthDataAsync();
    
    // Encryption helpers
    Task<string> EncryptAsync(string data, string key);
    Task<string?> DecryptAsync(string encryptedData, string key);
} 