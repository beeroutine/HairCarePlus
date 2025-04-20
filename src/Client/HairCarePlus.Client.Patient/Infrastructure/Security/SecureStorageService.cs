using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Security;

public class SecureStorageService : ISecureStorageService
{
    private const string AuthTokenKey = "auth_token";
    private const string TokenExpirationKey = "token_expiration";
    
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }

    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Remove(key);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task RemoveAllAsync()
    {
        try
        {
            SecureStorage.RemoveAll();
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        var value = await GetAsync(key);
        return value != null;
    }

    public async Task<string?> GetAuthTokenAsync()
    {
        return await GetAsync(AuthTokenKey);
    }

    public async Task SetAuthTokenAsync(string token)
    {
        await SetAsync(AuthTokenKey, token);
    }

    public async Task<DateTime?> GetTokenExpirationAsync()
    {
        var expirationStr = await GetAsync(TokenExpirationKey);
        if (expirationStr != null && DateTime.TryParse(expirationStr, out var expiration))
        {
            return expiration;
        }
        return null;
    }

    public async Task SetTokenExpirationAsync(DateTime expiration)
    {
        await SetAsync(TokenExpirationKey, expiration.ToString("O"));
    }

    public async Task ClearAuthDataAsync()
    {
        await RemoveAsync(AuthTokenKey);
        await RemoveAsync(TokenExpirationKey);
    }

    public async Task<string> EncryptAsync(string data, string key)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey(key);
        
        var iv = aes.IV;
        using var encryptor = aes.CreateEncryptor();
        
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        
        var result = new byte[iv.Length + encryptedData.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);
        
        return Convert.ToBase64String(result);
    }

    public async Task<string?> DecryptAsync(string encryptedData, string key)
    {
        try
        {
            var fullBytes = Convert.FromBase64String(encryptedData);
            
            using var aes = Aes.Create();
            var iv = new byte[aes.IV.Length];
            var cipherText = new byte[fullBytes.Length - iv.Length];
            
            Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullBytes, iv.Length, cipherText, 0, cipherText.Length);
            
            aes.Key = GetKey(key);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private byte[] GetKey(string key)
    {
        using var sha256 = SHA256.Create();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        return sha256.ComputeHash(keyBytes);
    }
} 