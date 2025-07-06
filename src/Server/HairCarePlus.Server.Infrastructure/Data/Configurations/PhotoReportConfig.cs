using HairCarePlus.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairCarePlus.Server.Infrastructure.Data.Configurations;

public class PhotoReportConfig : IEntityTypeConfiguration<PhotoReport>
{
    public void Configure(EntityTypeBuilder<PhotoReport> builder)
    {
        builder.ToTable("PhotoReports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.CaptureDate).IsRequired();
        builder.Property(r => r.ImageUrl).IsRequired();
        builder.Property(r => r.ThumbnailUrl).IsRequired(false);
        builder.Property(r => r.Notes).HasMaxLength(1024);

        // Owned type already configured in DbContext (AnalysisResult).

        // Relationship configured from the PhotoComment side to avoid duplicate shadow FKs.
    }
} 