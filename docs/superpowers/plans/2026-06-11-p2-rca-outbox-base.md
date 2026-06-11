# P2 RCA Outbox Base Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first P2 outbox persistence base for RCA integration events without replacing the existing derived event feed.

**Architecture:** Add a domain outbox entity, EF mapping/migration, and a small infrastructure service that persists `RcaDomainEventDto` payloads idempotently. Keep webhooks and feed replacement out of this first code cut.

**Tech Stack:** ASP.NET Core solution, C#/.NET, EF Core, MySQL/Pomelo, existing lightweight console tests in `tests/IshikawaRca.Tests`.

---

## File Structure

- Create `src/IshikawaRca.Domain/Enums/RcaOutboxEventStatus.cs`: status enum for persistence and retry state.
- Create `src/IshikawaRca.Domain/Entities/RcaOutboxEvent.cs`: tenant entity that stores the serialized event envelope and delivery state.
- Modify `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`: add `DbSet<RcaOutboxEvent>` and mapping/indexes.
- Create EF migration under `src/IshikawaRca.Infrastructure/Migrations`: add `rca_outbox_events`.
- Create `src/IshikawaRca.Application/Rca/IRcaOutboxService.cs`: application-facing outbox interface.
- Create `src/IshikawaRca.Infrastructure/Services/EfRcaOutboxService.cs`: EF implementation for idempotent enqueue and status transitions.
- Modify DI registration in `src/IshikawaRca.Web/Program.cs`: register the outbox service.
- Modify `tests/IshikawaRca.Tests/Program.cs`: add a lightweight outbox compatibility test using SQLite or EF in-memory if available; if not available, keep test limited to domain factory semantics and service-free serialization.
- Modify docs: `docs/backend.md`, `docs/chats/BACKEND.md`, `docs/VALIDATION_LOG.md`, `docs/ROADMAP.md`, `docs/STATUS_AND_NEXT_STEPS.md`.

## Task 1: Domain Outbox Model

**Files:**
- Create: `src/IshikawaRca.Domain/Enums/RcaOutboxEventStatus.cs`
- Create: `src/IshikawaRca.Domain/Entities/RcaOutboxEvent.cs`
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Add the failing domain test**

Add a method call near the existing test calls:

```csharp
AssertOutboxEventDomainDefaults();
```

Add this method:

```csharp
static void AssertOutboxEventDomainDefaults()
{
    var tenantId = Guid.NewGuid();
    var incidentId = Guid.NewGuid();
    var payload = "{\"type\":\"RcaIncidentCreated\"}";

    var outboxEvent = new RcaOutboxEvent
    {
        TenantId = tenantId,
        EventId = "rca-incident-created:" + incidentId,
        EventType = "RcaIncidentCreated",
        OccurredAt = DateTimeOffset.UtcNow,
        IncidentId = incidentId,
        PayloadJson = payload
    };

    if (outboxEvent.Status != RcaOutboxEventStatus.Pending ||
        outboxEvent.AttemptCount != 0 ||
        outboxEvent.NextAttemptAt is not null ||
        outboxEvent.PayloadJson != payload)
    {
        throw new InvalidOperationException("Expected outbox event defaults to preserve pending delivery state.");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
```

Expected: fail because `RcaOutboxEvent` and `RcaOutboxEventStatus` do not exist.

- [ ] **Step 3: Add minimal domain implementation**

Create `src/IshikawaRca.Domain/Enums/RcaOutboxEventStatus.cs`:

```csharp
namespace IshikawaRca.Domain.Enums;

public enum RcaOutboxEventStatus
{
    Pending = 0,
    Publishing = 1,
    Published = 2,
    Failed = 3,
    DeadLetter = 4
}
```

Create `src/IshikawaRca.Domain/Entities/RcaOutboxEvent.cs`:

```csharp
using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaOutboxEvent : TenantEntity
{
    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public Guid IncidentId { get; set; }

    public string? SourceSystem { get; set; }

    public string? ExternalTaskId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? ExternalWorkOrderId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public RcaOutboxEventStatus Status { get; set; } = RcaOutboxEventStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? LastError { get; set; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
```

Expected: build and tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/IshikawaRca.Domain/Enums/RcaOutboxEventStatus.cs src/IshikawaRca.Domain/Entities/RcaOutboxEvent.cs tests/IshikawaRca.Tests/Program.cs
git commit -m "feat(integration): add RCA outbox event model"
```

## Task 2: EF Mapping and Migration

**Files:**
- Modify: `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`
- Create: `src/IshikawaRca.Infrastructure/Migrations/*_AddRcaOutboxEvents.cs`
- Create: `src/IshikawaRca.Infrastructure/Migrations/*_AddRcaOutboxEvents.Designer.cs`
- Modify: `src/IshikawaRca.Infrastructure/Migrations/RcaDbContextModelSnapshot.cs`

- [ ] **Step 1: Add `DbSet` and mapping**

In `RcaDbContext`, add:

```csharp
public DbSet<RcaOutboxEvent> RcaOutboxEvents => Set<RcaOutboxEvent>();
```

Call the mapping from `OnModelCreating`:

```csharp
ConfigureRcaOutboxEvent(modelBuilder);
```

Add:

```csharp
private static void ConfigureRcaOutboxEvent(ModelBuilder modelBuilder)
{
    var entity = modelBuilder.Entity<RcaOutboxEvent>();

    entity.ToTable("rca_outbox_events");
    ConfigureTenantEntity(entity);

    entity.Property(x => x.EventId).HasMaxLength(220).IsRequired();
    entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
    entity.Property(x => x.SourceSystem).HasMaxLength(64);
    entity.Property(x => x.ExternalTaskId).HasMaxLength(120);
    entity.Property(x => x.ExternalEventId).HasMaxLength(120);
    entity.Property(x => x.ExternalWorkOrderId).HasMaxLength(120);
    entity.Property(x => x.PayloadJson).HasColumnType("json");
    entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
    entity.Property(x => x.LastError).HasMaxLength(2000);

    entity.HasIndex(x => new { x.TenantId, x.EventId }).IsUnique();
    entity.HasIndex(x => new { x.TenantId, x.Status, x.NextAttemptAt });
    entity.HasIndex(x => new { x.TenantId, x.IncidentId, x.OccurredAt });
    entity.HasIndex(x => new { x.TenantId, x.EventType, x.OccurredAt });
}
```

- [ ] **Step 2: Generate migration**

Run:

```powershell
dotnet ef migrations add AddRcaOutboxEvents --project src\IshikawaRca.Infrastructure\IshikawaRca.Infrastructure.csproj --startup-project src\IshikawaRca.Web\IshikawaRca.Web.csproj --no-build
```

Expected: migration files created with `rca_outbox_events`.

- [ ] **Step 3: Validate build**

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
```

Expected: build passes with 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs src/IshikawaRca.Infrastructure/Migrations
git commit -m "feat(db): add RCA outbox events table"
```

## Task 3: Outbox Service Base

**Files:**
- Create: `src/IshikawaRca.Application/Rca/IRcaOutboxService.cs`
- Create: `src/IshikawaRca.Infrastructure/Services/EfRcaOutboxService.cs`
- Modify: `src/IshikawaRca.Web/Program.cs`

- [ ] **Step 1: Create application interface**

```csharp
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;

namespace IshikawaRca.Application.Rca;

public interface IRcaOutboxService
{
    Task<RcaOutboxEvent> EnqueueAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RcaOutboxEvent>> ListPendingAsync(int take = 100, CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid id, string error, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implement EF service**

Create `EfRcaOutboxService` that:

- Serializes `RcaDomainEventDto` with `JsonSerializer`.
- Returns existing row when `TenantId + EventId` already exists.
- Lists `Pending` or `Failed` rows where `NextAttemptAt` is null or due.
- Marks `Published`.
- Marks `Failed`, increments `AttemptCount`, sets `LastAttemptAt`,
  `NextAttemptAt` and truncates `LastError` to 2000 chars.

- [ ] **Step 3: Register DI**

In `Program.cs`, register:

```csharp
builder.Services.AddScoped<IRcaOutboxService, EfRcaOutboxService>();
```

- [ ] **Step 4: Validate**

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
```

Expected: build and tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/IshikawaRca.Application/Rca/IRcaOutboxService.cs src/IshikawaRca.Infrastructure/Services/EfRcaOutboxService.cs src/IshikawaRca.Web/Program.cs
git commit -m "feat(integration): add RCA outbox service base"
```

## Task 4: Documentation Closure

**Files:**
- Modify: `docs/backend.md`
- Modify: `docs/chats/BACKEND.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/STATUS_AND_NEXT_STEPS.md`
- Modify: `docs/VALIDATION_LOG.md`

- [ ] **Step 1: Update docs**

Record:

- `RcaOutboxEvent` entity/table added.
- `IRcaOutboxService` and EF implementation added.
- Feed `/api/v1/integrations/rca/events` still uses derived events.
- Webhooks remain future work.
- Validation commands and result.

- [ ] **Step 2: Run final validation**

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
git diff --check
```

Expected: build/tests pass; diff check has no whitespace errors. CRLF warnings are acceptable on Windows.

- [ ] **Step 3: Commit**

```powershell
git add docs/backend.md docs/chats/BACKEND.md docs/ROADMAP.md docs/STATUS_AND_NEXT_STEPS.md docs/VALIDATION_LOG.md
git commit -m "docs(integration): record RCA outbox base"
```

## Self-Review

- Spec coverage: this plan covers the first outbox base only. It intentionally
  excludes automatic event capture, webhook delivery, operational endpoints and
  replacing the derived feed.
- Placeholder scan: no `TBD` or open implementation placeholders remain.
- Type consistency: `RcaOutboxEvent`, `RcaOutboxEventStatus` and
  `IRcaOutboxService` names are used consistently across tasks.
