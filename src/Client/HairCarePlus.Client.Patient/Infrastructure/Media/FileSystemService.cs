using System;
using System.IO;
using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Media;

public class FileSystemService : IFileSystemService
{
    private readonly string _cacheDirectory;
    private readonly string _mediaDirectory;

    public FileSystemService()
    {
        _cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "HairCarePlus");
        _mediaDirectory = Path.Combine(FileSystem.AppDataDirectory, "Media");
        
        // Ensure directories exist
        Directory.CreateDirectory(_cacheDirectory);
        Directory.CreateDirectory(_mediaDirectory);
    }

    public async Task<string> SaveFileAsync(byte[] data, string fileName, string directory)
    {
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, data);
        return filePath;
    }

    public async Task<byte[]?> ReadFileAsync(string filePath)
    {
        try
        {
            if (await FileExistsAsync(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (await FileExistsAsync(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }

    public async Task<long> GetFileSizeAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return await Task.FromResult(fileInfo.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<string> GetCacheDirectoryAsync()
    {
        return await Task.FromResult(_cacheDirectory);
    }

    public async Task<string> GetMediaDirectoryAsync()
    {
        return await Task.FromResult(_mediaDirectory);
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            var directory = new DirectoryInfo(_cacheDirectory);
            foreach (var file in directory.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                    // Log error if needed
                }
            }
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }

    public async Task<long> GetDirectorySizeAsync(string directory)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directory);
            return await Task.FromResult(
                directoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                    .Sum(file => file.Length)
            );
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<string[]> GetFilesInDirectoryAsync(string directory, string searchPattern = "*.*")
    {
        try
        {
            return await Task.FromResult(
                Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
            );
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }
} 