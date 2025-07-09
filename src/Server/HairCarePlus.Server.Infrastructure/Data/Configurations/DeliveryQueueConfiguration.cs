using HairCarePlus.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairCarePlus.Server.Infrastructure.Data.Configurations;

public class DeliveryQueueConfiguration : IEntityTypeConfiguration<DeliveryQueue>
{
    public void Configure(EntityTypeBuilder<DeliveryQueue> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EntityType)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(d => d.PayloadJson)
               .IsRequired();

        builder.HasIndex(d => d.PatientId);
        builder.HasIndex(d => d.ExpiresAtUtc);
    }
} 