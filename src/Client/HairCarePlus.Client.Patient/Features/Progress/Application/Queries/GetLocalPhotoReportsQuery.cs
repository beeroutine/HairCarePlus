using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities;
using HairCarePlus.Shared.Common.CQRS;
using System.Collections.Generic;

namespace HairCarePlus.Client.Patient.Features.Progress.Application.Queries;

public sealed record GetLocalPhotoReportsQuery : IQuery<IEnumerable<ProgressFeedItem>>; 