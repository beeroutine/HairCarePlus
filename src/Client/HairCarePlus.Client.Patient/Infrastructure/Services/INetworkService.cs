using System.Net.Http;

namespace HairCarePlus.Client.Patient.Infrastructure.Services
{
    public interface INetworkService
    {
        Task<T> GetAsync<T>(string endpoint, IDictionary<string, string>? headers = null);
        Task<T> PostAsync<T>(string endpoint, object data, IDictionary<string, string>? headers = null);
        Task<T> PutAsync<T>(string endpoint, object data, IDictionary<string, string>? headers = null);
        Task<T> DeleteAsync<T>(string endpoint, IDictionary<string, string>? headers = null);
        Task<bool> IsConnectedAsync();
        Task<HttpResponseMessage> UploadFileAsync(string endpoint, Stream fileStream, string fileName, string contentType, IDictionary<string, string>? headers = null);
        void SetAuthToken(string token);
    }
} 