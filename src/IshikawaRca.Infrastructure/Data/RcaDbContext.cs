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

    public DbSet<RcaEvidence> RcaEvidence => Set<RcaEvidence>();

    public DbSet<RcaFact> RcaFacts => Set<RcaFact>();

    public DbSet<RcaExternalIntakeRequest> RcaExternalIntakeRequests => Set<RcaExternalIntakeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRcaIncident(modelBuilder);
        ConfigureIshikawaBranch(modelBuilder);
        ConfigureIshikawaCause(modelBuilder);
        ConfigureCorrectiveAction(modelBuilder);
        ConfigureRcaEvidence(modelBuilder);
        ConfigureRcaFact(modelBuilder);
        ConfigureRcaExternalIntakeRequest(modelBuilder);
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
        entity.Property(x => x.ClaimActorType).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.ClaimOwnerName).HasMaxLength(160);
        entity.Property(x => x.ClosureSummary).HasMaxLength(4000);
        entity.Property(x => x.EscalatedTo8DByUserId).HasMaxLength(160);
        entity.Property(x => x.EscalationReason).HasMaxLength(4000);
        entity.Property(x => x.WizardStep).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.WizardStepCompletedByUserId).HasMaxLength(160);
        entity.Property(x => x.WizardStepNotes).HasColumnType("text");
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
        entity.HasIndex(x => new { x.TenantId, x.ClaimActorType, x.ClaimOwnerName });
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

        entity
            .HasMany(x => x.Evidence)
            .WithOne()
            .HasForeignKey(x => x.RcaIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasMany(x => x.Facts)
            .WithOne()
            .HasForeignKey(x => x.RcaIncidentId)
            .OnDelete(DeleteBehavior.Cascade);
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

    private static void ConfigureRcaEvidence(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RcaEvidence>();

        entity.ToTable("rca_evidence");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        entity.Property(x => x.EvidenceType).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SourceDetail).HasMaxLength(220);
        entity.Property(x => x.Tags).HasMaxLength(500);
        entity.Property(x => x.Summary).HasMaxLength(4000);
        entity.Property(x => x.ReferenceUri).HasMaxLength(1000);
        entity.Property(x => x.AttachmentFileName).HasMaxLength(260);
        entity.Property(x => x.AttachmentContentType).HasMaxLength(160);
        entity.Property(x => x.AttachmentStorageProvider).HasMaxLength(64);
        entity.Property(x => x.AttachmentStorageKey).HasMaxLength(500);
        entity.Property(x => x.AttachmentSha256).HasMaxLength(64);
        entity.Property(x => x.CapturedByUserId).HasMaxLength(160);
        entity.Property(x => x.ValidationStatus).HasMaxLength(32).IsRequired();
        entity.Property(x => x.ValidatedByUserId).HasMaxLength(160);
        entity.Property(x => x.ValidationNotes).HasMaxLength(2000);

        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId, x.CapturedAt });
        entity.HasIndex(x => new { x.TenantId, x.CauseId });
        entity.HasIndex(x => new { x.TenantId, x.ExternalIntakeId });
        entity.HasIndex(x => new { x.TenantId, x.ValidationStatus });
    }

    private static void ConfigureRcaExternalIntakeRequest(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RcaExternalIntakeRequest>();

        entity.ToTable("rca_external_intake_requests");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.ActorName).HasMaxLength(160);
        entity.Property(x => x.ContactName).HasMaxLength(160);
        entity.Property(x => x.ContactEmail).HasMaxLength(254);
        entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        entity.Property(x => x.ReviewedByUserId).HasMaxLength(160);
        entity.Property(x => x.RejectedByUserId).HasMaxLength(160);
        entity.Property(x => x.RejectionReason).HasMaxLength(1000);
        entity.Property(x => x.ClaimReference).HasMaxLength(160);
        entity.Property(x => x.MaterialCode).HasMaxLength(120);
        entity.Property(x => x.BatchOrLot).HasMaxLength(120);
        entity.Property(x => x.Description).HasColumnType("text");
        entity.Property(x => x.ContainmentResponse).HasColumnType("text");
        entity.Property(x => x.ProposedRootCause).HasColumnType("text");
        entity.Property(x => x.ProposedCorrectiveAction).HasColumnType("text");
        entity.Property(x => x.EvidenceSummary).HasColumnType("text");

        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId, x.Status });
        entity.HasIndex(x => new { x.TenantId, x.ActorType, x.ActorName });
    }

    private static void ConfigureRcaFact(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RcaFact>();

        entity.ToTable("rca_facts");
        ConfigureTenantEntity(entity);

        entity.Property(x => x.FactType).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SourceDetail).HasMaxLength(220);
        entity.Property(x => x.FactSeverity).HasMaxLength(32).HasDefaultValue("Info").IsRequired();
        entity.Property(x => x.ShiftCode).HasMaxLength(80);
        entity.Property(x => x.MachineCode).HasMaxLength(80);
        entity.Property(x => x.LineCode).HasMaxLength(80);
        entity.Property(x => x.WorkOrderCode).HasMaxLength(120);
        entity.Property(x => x.MaterialCode).HasMaxLength(120);
        entity.Property(x => x.BatchOrLot).HasMaxLength(120);
        entity.Property(x => x.AlarmCode).HasMaxLength(120);
        entity.Property(x => x.MeasurementName).HasMaxLength(160);
        entity.Property(x => x.MeasurementValue).HasPrecision(18, 6);
        entity.Property(x => x.MeasurementUnit).HasMaxLength(40);
        entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.CapturedByUserId).HasMaxLength(160);

        entity.HasIndex(x => new { x.TenantId, x.RcaIncidentId, x.OccurredAt });
        entity.HasIndex(x => new { x.TenantId, x.CauseId });
        entity.HasIndex(x => new { x.TenantId, x.EvidenceId });
        entity.HasIndex(x => new { x.TenantId, x.CorrectiveActionId });
        entity.HasIndex(x => new { x.TenantId, x.ExternalIntakeId });
        entity.HasIndex(x => new { x.TenantId, x.FactType });
        entity.HasIndex(x => new { x.TenantId, x.FactSeverity });
        entity.HasIndex(x => new { x.TenantId, x.MachineCode, x.OccurredAt });
        entity.HasIndex(x => new { x.TenantId, x.ShiftCode, x.OccurredAt });
        entity.HasIndex(x => new { x.TenantId, x.AlarmCode });
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
