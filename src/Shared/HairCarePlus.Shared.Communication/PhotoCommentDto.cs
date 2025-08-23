using System;

namespace HairCarePlus.Shared.Communication;

public sealed class PhotoCommentDto
{
    public Guid Id { get; set; }
    public Guid PhotoReportId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>
    ///   Optional stable identifier of the 3-photo set this comment belongs to.
    ///   Used to resolve cross-device id mismatches when PhotoReportId differs
    ///   between Patient and Clinic (e.g., comments created against locally
    ///   generated report ids in the Clinic app).
    /// </summary>
    public Guid? SetId { get; set; }

    /// <summary>
    ///   Optional photo type within the set to better target the exact item
    ///   when matching by SetId on the receiving side.
    /// </summary>
    public PhotoType? Type { get; set; }
} 