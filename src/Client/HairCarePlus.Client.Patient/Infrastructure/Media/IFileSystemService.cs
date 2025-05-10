using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Media;

public interface IMediaFileSystemService
{
    Task<bool> FileExistsAsync(string path);
    Task DeleteFileAsync(string path);
    Task<string> SaveFileAsync(byte[] data, string fileName, string directory);
    Task<byte[]?> ReadFileAsync(string path);
    Task<string> GetLocalPathAsync(string fileName, string directory);
    Task CleanupDirectoryAsync(string directory, int maxAgeInDays = 30);
    Task<string> GetCacheDirectoryAsync();
    Task<string> GetMediaDirectoryAsync();
} 