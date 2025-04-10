using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Security;

public interface ISecureStorageService
{
    Task<string> GetSecureAsync(string key);
    Task SetSecureAsync(string key, string value);
    Task RemoveSecureAsync(string key);
    Task<bool> ContainsKeyAsync(string key);
    Task<string> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task RemoveTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string> EncryptAsync(string data);
    Task<string> DecryptAsync(string encryptedData);
} 