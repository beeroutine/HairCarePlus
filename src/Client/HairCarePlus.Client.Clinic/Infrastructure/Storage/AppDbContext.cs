using HairCarePlus.Client.Clinic.Features.Chat.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HairCarePlus.Client.Clinic.Infrastructure.Storage
{
    public class AppDbContext : DbContext
    {
        private readonly string _dbPath;

        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        public AppDbContext()
        {
            // default ctor for design-time
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "clinic.db");
        }

        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}")
                          .EnableSensitiveDataLogging();
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
        }
    }
} 