# P4 Document Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete P4 in small cuts, starting with versioned RCA closure PDF documents and then adding storage, intake attachments and platform contracts.

**Architecture:** Keep Ishikawa RCA standalone. Add document governance as module-owned domain/application/infrastructure code, with storage hidden behind interfaces and future platform systems connected only by contracts.

**Tech Stack:** ASP.NET Core MVC/API, EF Core, MySQL migrations, existing `RcaPdfReportService`, existing `ApiResult<T>` contracts, existing audit and role patterns.

---

## File Structure

- `src/IshikawaRca.Domain/Enums/RcaClosureDocumentStatus.cs`: document approval status enum.
- `src/IshikawaRca.Domain/Entities/RcaClosureDocument.cs`: versioned closure document metadata.
- `src/IshikawaRca.Contracts/Rca/RcaClosureDocumentDto.cs`: public document version DTO.
- `src/IshikawaRca.Application/Rca/IRcaClosureDocumentService.cs`: application boundary for generate/list/approve/reject/download metadata.
- `src/IshikawaRca.Infrastructure/Services/EfRcaClosureDocumentService.cs`: EF implementation for metadata, versioning and audit.
- `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`: DbSet and mapping.
- `src/IshikawaRca.Web/Services/*`: reuse existing PDF bytes initially; introduce document storage in P4.2.
- `src/IshikawaRca.Web/Controllers/RcaController.cs`: MVC actions for generating/listing closure document versions.
- `src/IshikawaRca.Web/Controllers/Api/RcaDocumentsController.cs`: API read/governance actions when P4.1 contract is stable.
- `tests/IshikawaRca.Tests/Program.cs`: lightweight RED/GREEN coverage.
- `docs/backend.md`, `docs/API_CONTRACTS.md`, `docs/ROADMAP.md`, `docs/STATUS_AND_NEXT_STEPS.md`, `docs/VALIDATION_LOG.md`, `docs/chats/BACKEND.md`: closure docs per task.

---

### Task 1: Closure Document Domain Model

**Files:**
- Create: `src/IshikawaRca.Domain/Enums/RcaClosureDocumentStatus.cs`
- Create: `src/IshikawaRca.Domain/Entities/RcaClosureDocument.cs`
- Modify: `tests/IshikawaRca.Tests/Program.cs`
- Docs: `docs/VALIDATION_LOG.md`, `docs/backend.md`, `docs/chats/BACKEND.md`

- [ ] **Step 1: Write RED test**

Add a lightweight assertion called from top-level test runner:

```csharp
AssertRcaClosureDocumentDefaults();

static void AssertRcaClosureDocumentDefaults()
{
    var incidentId = Guid.NewGuid();
    var document = new RcaClosureDocument
    {
        TenantId = Guid.NewGuid(),
        RcaIncidentId = incidentId,
        Version = 1,
        FileName = "rca-closure-v1.pdf",
        ContentType = "application/pdf",
        SizeBytes = 128,
        StorageProvider = "Local",
        StorageKey = "closure/incident/v1.pdf",
        Sha256 = new string('a', 64),
        GeneratedByUserId = "quality"
    };

    if (document.Id == Guid.Empty ||
        document.RcaIncidentId != incidentId ||
        document.Status != RcaClosureDocumentStatus.Draft ||
        document.Version != 1)
    {
        throw new InvalidOperationException("Closure document defaults are invalid.");
    }
}
```

- [ ] **Step 2: Run RED**

Run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
```

Expected: compile failure because `RcaClosureDocument` and `RcaClosureDocumentStatus` do not exist.

- [ ] **Step 3: Add enum**

Create `RcaClosureDocumentStatus.cs`:

```csharp
namespace IshikawaRca.Domain.Enums;

public enum RcaClosureDocumentStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}
```

- [ ] **Step 4: Add entity**

Create `RcaClosureDocument.cs`:

```csharp
using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaClosureDocument : TenantEntity
{
    public Guid RcaIncidentId { get; set; }
    public int Version { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public RcaClosureDocumentStatus Status { get; set; } = RcaClosureDocumentStatus.Draft;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public string GeneratedByUserId { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
}
```

- [ ] **Step 5: Run GREEN**

Run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Expected: tests pass, build passes, diff check has no whitespace errors.

- [ ] **Step 6: Update docs and commit**

Commit:

```powershell
git add src/IshikawaRca.Domain/Enums/RcaClosureDocumentStatus.cs src/IshikawaRca.Domain/Entities/RcaClosureDocument.cs tests/IshikawaRca.Tests/Program.cs docs/backend.md docs/chats/BACKEND.md docs/VALIDATION_LOG.md
git commit -m "feat(docs): add RCA closure document domain model"
```

---

### Task 2: EF Mapping And Migration

**Files:**
- Modify: `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`
- Create migration: `src/IshikawaRca.Infrastructure/Data/Migrations/*_AddRcaClosureDocuments.cs`
- Modify: `tests/IshikawaRca.Tests/Program.cs`
- Docs: `docs/VALIDATION_LOG.md`, `docs/backend.md`, `docs/chats/BACKEND.md`

- [ ] **Step 1: Add mapping test intent**

Add a test that resolves `RcaDbContext` model metadata and verifies:

```csharp
AssertRcaClosureDocumentEfModel();
```

Expected table name: `rca_closure_documents`.
Expected indexes:

- `TenantId + RcaIncidentId + Version` unique.
- `TenantId + RcaIncidentId + GeneratedAt`.
- `TenantId + Status + GeneratedAt`.

- [ ] **Step 2: Run RED**

Run tests and expect failure because mapping does not exist.

- [ ] **Step 3: Add DbSet and mapping**

Add:

```csharp
public DbSet<RcaClosureDocument> RcaClosureDocuments => Set<RcaClosureDocument>();
```

Map max lengths:

- `FileName`: 260.
- `ContentType`: 160.
- `StorageProvider`: 64.
- `StorageKey`: 500.
- `Sha256`: 64.
- `GeneratedByUserId`: 160.
- `ReviewedByUserId`: 160.
- `ReviewNotes`: 2000.

- [ ] **Step 4: Generate migration**

Run after successful build:

```powershell
dotnet build IshikawaRca.sln /m:1
dotnet ef migrations add AddRcaClosureDocuments --no-build
```

- [ ] **Step 5: Validate and commit**

Run tests, build and diff check. Commit:

```powershell
git add src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs src/IshikawaRca.Infrastructure/Data/Migrations tests/IshikawaRca.Tests/Program.cs docs/backend.md docs/chats/BACKEND.md docs/VALIDATION_LOG.md
git commit -m "feat(db): add RCA closure document persistence"
```

---

### Task 3: Closure Document Service

**Files:**
- Create: `src/IshikawaRca.Contracts/Rca/RcaClosureDocumentDto.cs`
- Create: `src/IshikawaRca.Application/Rca/IRcaClosureDocumentService.cs`
- Create: `src/IshikawaRca.Infrastructure/Services/EfRcaClosureDocumentService.cs`
- Modify: `src/IshikawaRca.Infrastructure/DependencyInjection.cs`
- Modify: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: RED tests**

Add tests proving:

- First generated closure document for an incident gets version `1`.
- Second generated closure document gets version `2`.
- Non-closed RCA returns `RCA_NOT_CLOSED`.
- Approval changes status to `Approved` and writes audit.
- Rejection changes status to `Rejected` and writes audit.

- [ ] **Step 2: Add contracts**

Create DTO with id, incident id, version, filename, content type, size, sha,
status, generated/review metadata.

- [ ] **Step 3: Implement service**

Service methods:

```csharp
Task<ApiResult<RcaClosureDocumentDto>> RegisterGeneratedAsync(Guid incidentId, RegisterRcaClosureDocumentRequest request, CancellationToken cancellationToken = default);
Task<ApiResult<IReadOnlyList<RcaClosureDocumentDto>>> ListAsync(Guid incidentId, CancellationToken cancellationToken = default);
Task<ApiResult<RcaClosureDocumentDto>> ApproveAsync(Guid incidentId, Guid documentId, ReviewRcaClosureDocumentRequest request, CancellationToken cancellationToken = default);
Task<ApiResult<RcaClosureDocumentDto>> RejectAsync(Guid incidentId, Guid documentId, ReviewRcaClosureDocumentRequest request, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Validate and commit**

Run tests, build and diff check. Commit:

```powershell
git add src/IshikawaRca.Contracts/Rca src/IshikawaRca.Application/Rca src/IshikawaRca.Infrastructure/Services src/IshikawaRca.Infrastructure/DependencyInjection.cs tests/IshikawaRca.Tests/Program.cs docs
git commit -m "feat(backend): add RCA closure document service"
```

---

### Task 4: MVC/API Surface For Closure Documents

**Files:**
- Modify: `src/IshikawaRca.Web/Controllers/RcaController.cs`
- Create: `src/IshikawaRca.Web/Controllers/Api/RcaDocumentsController.cs`
- Modify: `src/IshikawaRca.Web/Views/Rca/Details.cshtml`
- Modify: `docs/API_CONTRACTS.md`

- [ ] **Step 1: Add controller tests where practical**

Use existing lightweight controller style to verify quality governance role
metadata on generate/approve/reject actions.

- [ ] **Step 2: Register document version on PDF generation**

When exporting a closed RCA PDF, compute SHA-256, register document version and
return the generated bytes. If registration fails, return controlled error.

- [ ] **Step 3: Add list/approve/reject API**

Expose:

```http
GET  /api/v1/rca/incidents/{id}/documents/closure
POST /api/v1/rca/incidents/{id}/documents/closure/{documentId}/approve
POST /api/v1/rca/incidents/{id}/documents/closure/{documentId}/reject
```

- [ ] **Step 4: Validate and commit**

Run tests, build and diff check. Commit:

```powershell
git add src/IshikawaRca.Web docs tests
git commit -m "feat(api): expose RCA closure document governance"
```

---

### Task 5: P4 Remaining Cut Specs

**Files:**
- Create specs/plans for P4.2, P4.3 and P4.4 under `docs/superpowers`.
- Modify: `docs/ROADMAP.md`, `docs/STATUS_AND_NEXT_STEPS.md`, `docs/chats/BACKEND.md`

- [ ] **Step 1: Write P4.2 storage boundary plan**

Cover local implementation first, DMS adapter later.

- [ ] **Step 2: Write P4.3 intake attachment plan**

Cover token scope, upload limits, hash, review, and optional import to RCA
evidence.

- [ ] **Step 3: Write P4.4 platform contract plan**

Cover Identity mapping, masters references, app registration, dashboards and
timeline as contracts only.

- [ ] **Step 4: Validate docs and commit**

Run:

```powershell
git diff --check
```

Commit:

```powershell
git add docs
git commit -m "docs(p4): plan remaining platform readiness cuts"
```

---

## Self-Review

- Spec coverage: P4 closure document versioning, storage boundary, intake
  attachments and platform contracts are covered.
- Scope: P4.1 is implementable without external systems; later cuts are split
  to avoid coupling.
- Placeholder scan: no task depends on an undefined external platform.
- Type consistency: task names use `RcaClosureDocument` consistently.
