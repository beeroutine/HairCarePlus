using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Chat.Domain.Entities;
using System;
using System.IO;
using HairCarePlus.Client.Patient.Features.Sync.Domain.Entities;
using HairCarePlus.Shared.Communication;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

public class AppDbContext : DbContext
{
    public DbSet<CalendarEvent> Events { get; set; }
    public DbSet<ChatMessageDto> ChatMessages { get; set; } = null!;
    public DbSet<OutboxItem> OutboxItems { get; set; }
    public DbSet<HairCarePlus.Client.Patient.Features.Sync.Domain.Entities.PhotoReportEntity> PhotoReports { get; set; } = null!;
    public DbSet<HairCarePlus.Client.Patient.Features.Sync.Domain.Entities.PhotoCommentEntity> PhotoComments { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event configuration
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ModifiedAt).IsRequired();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.TimeOfDay).IsRequired();
        });

        // Message configuration
        modelBuilder.Entity<ChatMessageDto>(entity =>
        {
            entity.ToTable("ChatMessages");
            
            entity.HasKey(e => e.LocalId);
            
            entity.HasIndex(e => e.ServerMessageId)
                .IsUnique()
                .HasFilter("[ServerMessageId] IS NOT NULL");
                
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.SyncStatus);
            
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(4000);
                
            entity.Property(e => e.SenderId)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.RecipientId)
                .HasMaxLength(50);
                
            entity.Property(e => e.FileName)
                .HasMaxLength(255);
                
            entity.Property(e => e.MimeType)
                .HasMaxLength(100);
                
            entity.HasOne(e => e.ReplyTo)
                .WithMany()
                .HasForeignKey(e => e.ReplyToLocalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Outbox configuration – keeps pending entities for sync
        modelBuilder.Entity<OutboxItem>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.Status);
            entity.Property(o => o.EntityType).IsRequired();
            entity.Property(o => o.PayloadJson).IsRequired();
            entity.Property(o => o.LocalEntityId).IsRequired();
            entity.Property(o => o.ModifiedAtUtc).IsRequired();
        });

        modelBuilder.Entity<OutboxItem>()
            .HasIndex(o => new { o.Status, o.CreatedAtUtc });

        // PhotoReport configuration
        modelBuilder.Entity<HairCarePlus.Client.Patient.Features.Sync.Domain.Entities.PhotoReportEntity>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasMany(p => p.Comments)
                  .WithOne()
                  .HasForeignKey(c => c.PhotoReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HairCarePlus.Client.Patient.Features.Sync.Domain.Entities.PhotoCommentEntity>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.PhotoReportId);
        });
    }
} 