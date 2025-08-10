using System;

namespace HairCarePlus.Shared.Communication;

public sealed class EntityHeaderDto
{
    public Guid Id { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
} 