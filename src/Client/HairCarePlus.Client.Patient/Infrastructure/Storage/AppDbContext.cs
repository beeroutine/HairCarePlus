using Microsoft.EntityFrameworkCore;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Chat.Models;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage;

public class AppDbContext : DbContext
{
    public DbSet<CalendarEvent> Events { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }

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
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
            entity.Property(e => e.SenderId).IsRequired();
        });
    }
} 