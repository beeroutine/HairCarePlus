using HairCarePlus.Client.Clinic.Features.Chat.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities;

namespace HairCarePlus.Client.Clinic.Infrastructure.Storage
{
    public class AppDbContext : DbContext
    {
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoReportEntity> PhotoReports => Set<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoReportEntity>();
        public DbSet<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoCommentEntity> PhotoComments => Set<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoCommentEntity>();
        public DbSet<OutboxItem> OutboxItems { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.LocalId);
                entity.Property(e => e.ConversationId).HasMaxLength(50);
                entity.Property(e => e.SentAt)
                    .HasConversion(new DateTimeOffsetToBinaryConverter());
                entity.ToTable("ChatMessages");
            });

            modelBuilder.Entity<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoReportEntity>(entity =>
            {
                entity.HasMany(p => p.Comments)
                      .WithOne()
                      .HasForeignKey(c => c.PhotoReportId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HairCarePlus.Client.Clinic.Features.Sync.Domain.Entities.PhotoCommentEntity>(entity =>
            {
                entity.HasIndex(c => c.PhotoReportId);
            });

            modelBuilder.Entity<OutboxItem>()
                .HasIndex(o => new { o.Status, o.CreatedAtUtc });
        }
    }
} 