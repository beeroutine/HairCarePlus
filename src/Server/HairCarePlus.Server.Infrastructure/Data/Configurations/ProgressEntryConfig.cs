using HairCarePlus.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairCarePlus.Server.Infrastructure.Data.Configurations;

public class ProgressEntryConfig : IEntityTypeConfiguration<ProgressEntry>
{
    public void Configure(EntityTypeBuilder<ProgressEntry> builder)
    {
        builder.ToTable("ProgressEntries");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.PatientId).IsRequired();
        builder.Property(p => p.DateUtc).IsRequired();
        builder.Property(p => p.CompletedTasks).IsRequired();
        builder.Property(p => p.TotalTasks).IsRequired();

        builder.HasIndex(p => new { p.PatientId, p.DateUtc }).IsUnique();
    }
} 