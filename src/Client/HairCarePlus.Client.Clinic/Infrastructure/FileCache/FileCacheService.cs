using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace HairCarePlus.Client.Clinic.Infrastructure.FileCache;

public sealed class FileCacheService : IFileCacheService
{
    private readonly HttpClient _http;
    private readonly ILogger<FileCacheService> _logger;
    private readonly string _rootDir;

    public FileCacheService(HttpClient http, ILogger<FileCacheService> logger)
    {
        _http = http;
        _logger = logger;
        _rootDir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        Directory.CreateDirectory(_rootDir);
    }

    public async Task<string> GetLocalPathAsync(string remoteUrl, CancellationToken cancellationToken = default)
    {
        var localPath = GetPathForUrl(remoteUrl);
        if (File.Exists(localPath)) return localPath;

        await DownloadAsync(remoteUrl, localPath, cancellationToken);
        return localPath;
    }

    public async Task PrefetchAsync(IEnumerable<string> remoteUrls, CancellationToken cancellationToken = default)
    {
        foreach (var url in remoteUrls)
        {
            var path = GetPathForUrl(url);
            if (!File.Exists(path))
            {
                try { await DownloadAsync(url, path, cancellationToken); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to prefetch {Url}", url);
                }
            }
        }
    }

    private string GetPathForUrl(string url)
    {
        var hash = Sha1(url);
        var ext = Path.GetExtension(new Uri(url).LocalPath);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        return Path.Combine(_rootDir, $"{hash}{ext}");
    }

    private async Task DownloadAsync(string url, string path, CancellationToken ct)
    {
        _logger.LogInformation("Downloading {Url} to cache", url);
        var bytes = await _http.GetByteArrayAsync(url, ct);
        await File.WriteAllBytesAsync(path, bytes, ct);
    }

    private static string Sha1(string input)
    {
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
} 