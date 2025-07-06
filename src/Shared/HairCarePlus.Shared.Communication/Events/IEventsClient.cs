using System.Threading.Tasks;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Shared.Communication.Events;
 
public interface IEventsClient
{
    Task PhotoReportAdded(string patientId, PhotoReportDto report);
    Task PhotoCommentAdded(string patientId, string photoReportId, PhotoCommentDto comment);
} 