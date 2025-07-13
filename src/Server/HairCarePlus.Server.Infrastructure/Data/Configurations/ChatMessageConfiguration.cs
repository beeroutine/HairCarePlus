using HairCarePlus.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairCarePlus.Server.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
               .IsRequired();

        builder.Property(c => c.SenderId)
               .IsRequired();

        builder.Property(c => c.ReceiverId)
               .IsRequired();

        builder.Property(c => c.Type)
               .HasConversion<int>();

        builder.Property(c => c.Status)
               .HasConversion<int>();

        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.SenderId);
        builder.HasIndex(c => c.ReceiverId);
    }
} 