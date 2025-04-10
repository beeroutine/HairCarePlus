using System.IO;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Media;

public interface IFileSystemService
{
    Task<string> SaveFileAsync(string fileName, Stream content);
    Task<Stream> GetFileAsync(string fileName);
    Task<bool> DeleteFileAsync(string fileName);
    Task<bool> FileExistsAsync(string fileName);
    Task<string> GetFilePath(string fileName);
    Task<long> GetFileSizeAsync(string fileName);
    Task ClearTempFilesAsync();
    Task<string> GetCacheFolderPath();
    Task<long> GetAvailableSpaceAsync();
    Task<bool> EnsureStorageAvailableAsync(long requiredSpace);
} 