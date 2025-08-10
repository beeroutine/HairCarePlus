using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Shared.Domain.Restrictions;

public interface IRestrictionRepository
{
    Task<IReadOnlyList<Restriction>> GetActiveAsync(DateTime today, CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal domain entity (can be extended later)
/// </summary>
public record Restriction(string Title, RestrictionIconType IconType, DateTime EndDate, string? Description); 