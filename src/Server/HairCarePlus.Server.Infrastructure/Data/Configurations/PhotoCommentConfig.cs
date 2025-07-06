using HairCarePlus.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairCarePlus.Server.Infrastructure.Data.Configurations;

public class PhotoCommentConfig : IEntityTypeConfiguration<PhotoComment>
{
    public void Configure(EntityTypeBuilder<PhotoComment> builder)
    {
        builder.ToTable("PhotoComments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.Text).IsRequired().HasMaxLength(1024);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        // Shadow properties generated in older migrations, ignore to prevent duplicate FKs
        builder.Ignore("PhotoReportId1");
        builder.Ignore("PhotoReportId2");

        // Explicit FK mapping to avoid duplicate shadow property
        builder.HasOne(c => c.PhotoReport)
               .WithMany(r => r.Comments)
               .HasForeignKey(c => c.PhotoReportId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
} 