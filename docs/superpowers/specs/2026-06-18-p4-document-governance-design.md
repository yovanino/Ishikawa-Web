# P4 Document Governance And Platform Readiness Design

Date: 2026-06-18.

## Goal

Complete P4 in independent cuts that move Ishikawa RCA toward corporate
document governance and platform readiness without coupling this repository to
future global modules.

## Scope

P4 includes:

- Versioned closure PDF records for closed RCA incidents.
- A stable document manifest for closure artifacts and evidence attachments.
- Internal approval/firma state for closure documents.
- A replaceable document storage boundary that keeps local storage working.
- Contract documentation for future Identity, customer/supplier masters, app
  global registration, cross-module dashboards and unified timeline.

P4 excludes direct dependencies on:

- Corporate Identity tables or SDKs.
- Global customer/supplier master tables.
- A concrete DMS vendor.
- The future app global database.
- Cross-module dashboard implementation inside this repo.

Those systems must consume this module by versioned APIs, events, snapshots or
document contracts.

## Cut Sequence

### P4.1 - Closure Document Versions

Add a persistent `RcaClosureDocument` record for each generated closure PDF.
The record stores version number, generated file name, storage metadata,
SHA-256, generation user/date and approval state. Existing PDF export remains
usable, but closed RCA exports can be registered as auditable document versions.

Initial approval states:

- `Draft`
- `PendingApproval`
- `Approved`
- `Rejected`

P4.1 does not implement electronic signature. It records approval intent and
keeps the future signature integration separate.

### P4.2 - Document Storage Boundary

Introduce an application-facing document storage interface so closure PDFs and
future corporate DMS documents do not depend on MVC internals. The first
implementation writes to local storage and preserves standalone operation.

### P4.3 - Intake External Attachments

Add controlled binary attachments for external intake responses. Attachments
remain scoped to a tokenized intake, are reviewed internally, and are not
automatically incorporated as official RCA evidence without human action.

### P4.4 - Platform Contracts

Document and expose read-only contracts for:

- Corporate Identity mapping.
- Global customer/supplier master references.
- App global module registration.
- Cross-module dashboard summaries.
- Unified operational timeline consumption.

The implementation must remain adapters/contracts only; no direct dependency on
other module tables.

## Architecture

The module keeps local standalone behavior as the default. New document
features live behind application interfaces and EF entities owned by this
module. Future platform systems integrate by replacing storage implementations
or consuming versioned APIs/events.

Closure document generation should reuse the existing `RcaPdfReportService` for
bytes and add a persistence/service layer for document version metadata.

## Data Rules

- Document versions are append-only for a given RCA.
- A new generated PDF increments version by tenant + incident.
- SHA-256 is required for every stored document.
- Document approval/rejection must be audited.
- Deleting a source evidence attachment must not erase historical closure
  document metadata.

## Security

- Generating closure document versions requires the same quality governance role
  used for closure/audit operations.
- Downloading a closure document version requires authenticated access and
  incident visibility.
- Approval/rejection requires quality governance role.
- Future Identity integration may replace standalone roles, but P4 must not
  depend on it.

## Validation Strategy

Each cut must run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Smoke tests that start the web app must use explicit startup/shutdown timeouts
and verify no local process remains alive.

## Risks

- Storing generated PDFs without version metadata weakens auditability; P4.1
  fixes this first.
- Implementing Identity or masters directly would violate module boundaries.
- A DMS-specific integration too early would make local standalone operation
  brittle.
- Approval labels could imply legal electronic signature. P4 uses internal
  approval state only until a signature policy is defined.
