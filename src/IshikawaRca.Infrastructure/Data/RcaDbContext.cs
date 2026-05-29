using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Data;

public class RcaDbContext : DbContext
{
    public RcaDbContext(DbContextOptions<RcaDbContext> options)
        : base(options)
    {
    }

    public DbSet<RcaIncident> RcaIncidents => Set<RcaIncident>();

    public DbSet<IshikawaBranch> IshikawaBranches => Set<IshikawaBranch>();

    public DbSet<IshikawaCause> IshikawaCauses => Set<IshikawaCause>();

    public DbSet<CorrectiveAction> CorrectiveActions => Set<CorrectiveAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRcaIncident(modelBuilder);
        ConfigureIshikawaBranch(modelBuilder);
        ConfigureIshikawaCause(modelBuilder);
        ConfigureCorrectiveAction(modelBuilder);
    }

    private static void ConfigureRcaIncident(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RcaIncident>();

        entity.ToTable("rca_incidents");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        entity.Property(x => x.ProblemDescription).HasMaxLength(4000);
        entity.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.ClaimScope).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.ClaimOwnerName).HasMaxLength(160);
        entity.Property(x => x.ClosureSummary).HasMaxLength(4000);
        entity.Property(x => x.SourceSystem).HasMaxLength(64).IsRequired();
        entity.Property(x => x.ExternalTaskId).HasMaxLength(120);
        entity.Property(x => x.ExternalEventId).HasMaxLength(120);
        entity.Property(x => x.ExternalWorkOrderId).HasMaxLength(120);
        entity.Property(x => x.MachineCode).HasMaxLength(80);
        entity.Property(x => x.LineCode).HasMaxLength(80);
        entity.Property(x => x.WorkOrderCode).HasMaxLength(120);
        entity.Property(x => x.ReportedBy).HasMaxLength(160);
        entity.Property(x => x.TaskSnapshotJson).HasColumnType("json");
        entity.Property(x => x.ContextSnapshotJson).HasColumnType("json");

        entity.HasIndex(x => new { x.TenantId, x.Status, x.Severity });
        entity.HasIndex(x => new { x.TenantId, x.ClaimScope, x.ClaimOwnerName });
        entity.HasIndex(x => new { x.TenantId, x.SourceSystem, x.ExternalTaskId });
        entity.HasIndex(x => new { x.TenantId, x.MachineCode, x.OccurredAt });

        entity
            .HasMany(x => x.Branches)
            .WithOne()
            .HasForeignKey(x => x.RcaIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(x => x.CorrectiveActions)
            .WithOne()
            .HasForeignKey(x => x.RcaIncidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIshikawaBranch(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IshikawaBranch>();

        entity.ToTable("ishikawa_branches");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.Color).HasMaxLength(32);

        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId, x.Order });

        entity
            .HasMany(x => x.Causes)
            .WithOne()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIshikawaCause(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IshikawaCause>();

        entity.ToTable("ishikawa_causes");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.EvidenceSummary).HasMaxLength(4000);
        entity.Property(x => x.X).HasPrecision(10, 2);
        entity.Property(x => x.Y).HasPrecision(10, 2);

        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId });
        entity.HasIndex(x => new { x.TenantId, x.ParentCauseId });
        entity.HasIndex(x => new { x.TenantId, x.IsRootCause });
    }

    private static void ConfigureCorrectiveAction(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CorrectiveAction>();

        entity.ToTable("corrective_actions");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.AssignedToUserId).HasMaxLength(160);
        entity.Property(x => x.CompletedByUserId).HasMaxLength(160);
        entity.Property(x => x.ValidationNotes).HasMaxLength(4000);

        entity.HasIndex(x => new { x.TenantId, x.Status, x.DueDate });
        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId });
        entity.HasIndex(x => new { x.TenantId, x.CauseId });
    }

    private static void ConfigureTenantEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : TenantEntity
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasCharSet("ascii").HasMaxLength(36);
        entity.Property(x => x.TenantId).HasCharSet("ascii").HasMaxLength(36);
        entity.Property(x => x.CreatedByUserId).HasMaxLength(160);
        entity.Property(x => x.UpdatedByUserId).HasMaxLength(160);
        entity.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
