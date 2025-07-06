using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Clinic.Infrastructure.Features.Progress;

public interface IPhotoReportService
{
    Task<IReadOnlyList<PhotoReportDto>> GetReportsAsync(string patientId);

    Task<PhotoCommentDto> AddCommentAsync(string patientId, string photoReportId, string authorId, string text);

    /// <summary>
    /// Connects to realtime events hub for given patient.
    /// </summary>
    Task ConnectAsync(string patientId);
} 