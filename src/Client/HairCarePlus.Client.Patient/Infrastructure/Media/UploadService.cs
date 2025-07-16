using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HairCarePlus.Shared.Common;

namespace HairCarePlus.Client.Patient.Infrastructure.Media
{
    public class UploadService : IUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UploadService> _logger;

        public UploadService(ILogger<UploadService> logger)
        {
            _httpClient = new HttpClient { BaseAddress = new System.Uri(EnvironmentHelper.GetBaseApiUrl()) };
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(string filePath, string fileName)
        {
            try
            {
                await using var fileStream = File.OpenRead(filePath);
                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileStream), "file", fileName);

                var response = await _httpClient.PostAsync("/api/files/upload", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                return result?.Url ?? string.Empty;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "File upload failed for {FilePath}", filePath);
                return string.Empty;
            }
        }

        private class UploadResult
        {
            public string Url { get; set; }
        }
    }
} 