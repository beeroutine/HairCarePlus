using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;

namespace HairCarePlus.Client.Patient.Features.Sync.Messages;
 
public sealed record PhotoReportSyncedMessage(PhotoReportEntity Report);
public sealed record PhotoCommentSyncedMessage(PhotoCommentEntity Comment); 