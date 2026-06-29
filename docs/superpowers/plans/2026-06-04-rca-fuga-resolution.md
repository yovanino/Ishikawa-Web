# RCA Fuga Resolution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a full resolution stage that separates root-cause actions from FUGA/no-detection actions.

**Architecture:** Keep the existing corrective action entity, but classify each action by type and resolution scope. A small domain policy evaluates closure/wizard prerequisites so service, API and UI share the same rule language.

**Tech Stack:** .NET 9, ASP.NET Core MVC, EF Core with MySQL, Razor views, a lightweight console regression test project.

---

### Task 1: Resolution Policy Test

**Files:**
- Create: `tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`
- Create: `tests/IshikawaRca.Tests/Program.cs`
- Modify: `IshikawaRca.sln`

- [ ] Write a failing console test that requires action type, resolution scope and closure blockers.
- [ ] Run `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj --no-restore` and confirm it fails because the policy/enums do not exist.

### Task 2: Domain Classification

**Files:**
- Create: `src/IshikawaRca.Domain/Enums/CorrectiveActionType.cs`
- Create: `src/IshikawaRca.Domain/Enums/RcaResolutionScope.cs`
- Create: `src/IshikawaRca.Domain/Services/RcaResolutionPolicy.cs`
- Modify: `src/IshikawaRca.Domain/Entities/CorrectiveAction.cs`

- [ ] Add action type values `Corrective`, `Preventive`, `RecurrencePreventive`.
- [ ] Add scope values `RootCause` and `Escape`.
- [ ] Add policy blockers for missing root-cause recurrence action and missing escape action set when escape is documented.

### Task 3: Contracts, Persistence and Service

**Files:**
- Modify: `src/IshikawaRca.Contracts/Rca/AddCorrectiveActionRequest.cs`
- Modify: `src/IshikawaRca.Contracts/Rca/CorrectiveActionDto.cs`
- Modify: `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`
- Modify: `src/IshikawaRca.Infrastructure/Services/EfRcaIncidentService.cs`

- [ ] Carry action type and scope through create/list APIs.
- [ ] Configure EF string conversions with defaults and indexes.
- [ ] Use the domain policy in close and wizard validation.

### Task 4: MVC UI

**Files:**
- Modify: `src/IshikawaRca.Web/Models/Rca/AddCorrectiveActionViewModel.cs`
- Modify: `src/IshikawaRca.Web/Models/Rca/RcaIncidentDetailsViewModel.cs`
- Modify: `src/IshikawaRca.Web/Controllers/RcaController.cs`
- Modify: `src/IshikawaRca.Web/Views/Rca/Details.cshtml`

- [ ] Add type/scope selectors.
- [ ] Split action display into root-cause resolution and FUGA/no-detection resolution.
- [ ] Show clear operational text for recurrence prevention requirements.

### Task 5: Migration and Verification

**Files:**
- Create: EF migration under `src/IshikawaRca.Infrastructure/Data/Migrations`

- [ ] Add migration for `ActionType` and `ResolutionScope`.
- [ ] Run console tests.
- [ ] Run solution build.
