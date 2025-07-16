using System.Threading.Tasks;

namespace HairCarePlus.Client.Patient.Infrastructure.Media
{
    public interface IUploadService
    {
        Task<string> UploadFileAsync(string filePath, string fileName);
    }
} 