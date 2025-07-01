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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities to use soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda<Func<object, bool>>(
                        Expression.Not(
                            Expression.Call(
                                typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool)),
                                Expression.Convert(Expression.Parameter(typeof(object), "e"), typeof(BaseEntity)),
                                Expression.Constant(nameof(BaseEntity.IsDeleted))
                            )
                        ),
                        Expression.Parameter(typeof(object), "e")
                    ));
            }
        }

        // Apply entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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