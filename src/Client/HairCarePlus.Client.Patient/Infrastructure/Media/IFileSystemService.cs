using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Media;

public interface IFileSystemService
{
    Task<string> SaveFileAsync(byte[] data, string fileName, string directory);
    Task<byte[]?> ReadFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task<long> GetFileSizeAsync(string filePath);
    Task<string> GetCacheDirectoryAsync();
    Task<string> GetMediaDirectoryAsync();
    Task ClearCacheAsync();
    Task<long> GetDirectorySizeAsync(string directory);
    Task<string[]> GetFilesInDirectoryAsync(string directory, string searchPattern = "*.*");
} 