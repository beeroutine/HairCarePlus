using System;
using System.Threading;
using System.Threading.Tasks;
using HairCarePlus.Server.Domain.Entities;
using HairCarePlus.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HairCarePlus.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Clinic> Clinics { get; set; } = null!;
    public DbSet<ClinicStaff> ClinicStaff { get; set; } = null!;
    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<TreatmentSchedule> TreatmentSchedules { get; set; } = null!;
    public DbSet<PhotoReport> PhotoReports { get; set; } = null!;
    public DbSet<HairCarePlus.Server.Domain.ValueObjects.PhotoComment> PhotoComments { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ProgressEntry> ProgressEntries { get; set; } = null!;
    public DbSet<Restriction> Restrictions { get; set; } = null!;
    public DbSet<DeliveryQueue> DeliveryQueue { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities to use soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var castToBase = Expression.Convert(parameter, typeof(BaseEntity));
                var prop = Expression.Property(castToBase, nameof(BaseEntity.IsDeleted));
                var body = Expression.Not(prop);

                var filter = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        // Apply entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // TimeZoneInfo is a complex CLR type; store separately or ignore for now
        modelBuilder.Entity<Clinic>().Ignore(c => c.TimeZone);
        modelBuilder.Entity<Doctor>().Ignore(d => d.TimeZone);
        modelBuilder.Entity<Patient>().Ignore(p => p.TimeZone);

        // Configure value objects / owned types so EF doesn't expect PKs
        modelBuilder.Entity<Clinic>(clinicBuilder =>
        {
            // Single owned object
            clinicBuilder.OwnsOne(c => c.Settings, settingsBuilder =>
            {
                settingsBuilder.OwnsOne(s => s.NotificationSettings);
                settingsBuilder.OwnsOne(s => s.AISettings);
            });

            // Collection of value objects (owned) – EF will create shadow key automatically
            clinicBuilder.OwnsMany(c => c.WorkingHours);
        });

        // Configure value object inside PhotoReport
        modelBuilder.Entity<PhotoReport>(reportBuilder =>
        {
            reportBuilder.OwnsOne(r => r.AnalysisResult);
        });

        // Configure value objects inside TreatmentSchedule
        modelBuilder.Entity<TreatmentSchedule>(scheduleBuilder =>
        {
            scheduleBuilder.OwnsOne(s => s.RecurrencePattern);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Update(); // This will set both CreatedAt and UpdatedAt
                    break;
                case EntityState.Modified:
                    entry.Entity.Update(); // This will set UpdatedAt
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
} 