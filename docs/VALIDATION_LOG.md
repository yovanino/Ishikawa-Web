# Validation Log

## 2026-06-10 - Local validation SDK preflight

Scope: validate that local smoke/build scripts fail fast when the required
.NET SDK is not available.

Checks:

- Added `scripts/check-dotnet-sdk.ps1` to read `global.json` and verify a
  registered matching .NET SDK.
- Added `-Build` to `scripts/run-local-validation.ps1` and pass-through to
  `start-web.ps1`.
- `scripts/start-web.ps1` now checks SDK availability before build, and before
  asking for a compiled DLL when none exists.

Validation:

- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\check-dotnet-sdk.ps1`: failed fast as expected with
  `No .NET SDKs are registered. Install SDK 10.0.300 or update global.json to
  an installed SDK.`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 5 -RequestTimeoutSeconds 5 -ShutdownTimeoutSeconds 5`:
  failed fast through the same preflight and reported no PID file to stop.

Result: passed for the SDK-preflight behavior; full build/smoke remains blocked
until SDK `10.0.300` is installed or `global.json` is aligned.

## 2026-06-10 - API authorization error normalization

Scope: validate consistent API error responses for authentication and
authorization failures.

Checks:

- Added an API authorization result handler for `/api` requests.
- API authentication challenges return HTTP 401 with `ApiResult<object>` and
  error code `AUTHENTICATION_REQUIRED`.
- API forbidden results return HTTP 403 with `ApiResult<object>` and error code
  `FORBIDDEN`.
- MVC routes keep the default ASP.NET Core authorization behavior.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: blocked because the local `dotnet`
  host reports no installed SDKs and `global.json` requests SDK `10.0.300`.
- `dotnet --info`: blocked for build purposes; only .NET runtimes are
  registered, no SDKs.
- Visual Studio MSBuild fallback: blocked because `Microsoft.NET.Sdk` and
  `Microsoft.NET.Sdk.Web` cannot be resolved without a registered SDK.
- Static diff/reference review completed for the new authorization handler,
  DI registration and API contract documentation.

Result: code change prepared; runtime/build validation blocked by missing local
.NET SDK.

## 2026-06-08 - Backend standalone auth context

Scope: validate the first P0 backend security increment.

Checks:

- Added standalone authentication configuration through `RcaSecurity`.
- Added current RCA user context for tenant/user resolution.
- Replaced MVC hardcoded `DemoTenantId` with configured/authenticated tenant.
- Added role protection for sensitive MVC/API operations: action status,
  evidence validation/update, evidence attachment replacement/deletion, RCA
  closure, 8D escalation and internal external-intake management.
- API incident creation now uses the authenticated/configured tenant when the
  request omits `tenantId`.
- Build passed with 0 errors.
- Lightweight tests passed.

Validation:

- `dotnet build IshikawaRca.sln`: passed with 0 errors and 4 `NU1900`
  warnings because package vulnerability metadata could not be fetched from
  `https://api.nuget.org/v3/index.json` in the restricted environment.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  passed.

Result: passed.

## 2026-06-08 - Backend sensitive operation audit records

Scope: validate the first P0 backend audit increment.

Checks:

- Added domain entity `RcaAuditRecord`.
- Added EF mapping and `RcaDbContext.RcaAuditRecords`.
- Generated EF migration `AddRcaAuditRecords` with table
  `rca_audit_records` and indexes by tenant/incident/time, entity and action.
- Added audit writes for corrective action status changes.
- Added audit writes for evidence update, attachment replacement and logical
  deletion.
- Added audit writes for RCA closure and 8D escalation.
- Added audit writes for internal external-intake review, rejection and
  revocation.

Validation:

- `dotnet ef migrations add AddRcaAuditRecords --project
  src/IshikawaRca.Infrastructure/IshikawaRca.Infrastructure.csproj
  --startup-project src/IshikawaRca.Web/IshikawaRca.Web.csproj`: passed.
- `dotnet build IshikawaRca.sln`: passed with 0 errors and 4 `NU1900`
  warnings because package vulnerability metadata could not be fetched from
  `https://api.nuget.org/v3/index.json` in the restricted environment.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  passed.

Database:

- Initial attempts without `ISHIKAWA_RCA_CONNECTION` used the placeholder
  `ishikawa_user` connection and failed with `Access denied`.
- Migration `20260608140016_AddRcaAuditRecords` applied successfully after
  setting `ISHIKAWA_RCA_CONNECTION` from local development configuration.

Result: passed.

## 2026-06-08 - Evidence storage hardening

Scope: validate local evidence attachment storage hardening.

Checks:

- Added configurable `EvidenceStorage:MaxFileSizeMb`, defaulting to 100 MB.
- Preserved allowed extension validation.
- Hardened storage key resolution/deletion with `Path.GetRelativePath` to
  ensure resolved files stay inside the configured storage root.
- Added lightweight tests for oversized attachment rejection and unsafe storage
  key rejection.

Validation:

- `dotnet build IshikawaRca.sln`: passed with 0 errors and 4 `NU1900`
  warnings because package vulnerability metadata could not be fetched from
  `https://api.nuget.org/v3/index.json` in the restricted environment.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  passed.

Result: passed.

## 2026-06-08 - API error normalization

Scope: validate consistent API error responses for model validation and
unhandled exceptions.

Checks:

- Added `ApiErrorResponseFactory` for automatic model-state failures in API
  controllers.
- Added `/api` exception middleware returning `ApiResult<object>` with
  `UNHANDLED_API_ERROR` and `correlationId`.
- Documented the behavior in `docs/API_CONTRACTS.md`.

Validation:

- `dotnet build IshikawaRca.sln`: passed with 0 errors and 4 `NU1900`
  warnings because package vulnerability metadata could not be fetched from
  `https://api.nuget.org/v3/index.json` in the restricted environment.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  passed.

Result: passed.

## 2026-06-08 - Backend critical smoke API + DB

Scope: validate the P0 critical API smoke flow against local DB.

Checks:

- Updated `scripts/start-web.ps1` to start the compiled Web DLL correctly when
  the repository path contains spaces, force `ASPNETCORE_ENVIRONMENT=Development`
  for local validation and emit stdout/stderr logs on startup failure.
- Updated `scripts/smoke-test.ps1` to send standalone auth headers.
- Smoke flow now creates an RCA, adds root cause and subcause, records validated
  evidence and validated evidence file, creates/completes corrective and
  recurrence-preventive root-cause actions, escalates to 8D, closes the RCA,
  completes wizard closed step, validates snapshot/events and calls AI summary.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 errors and 4 `NU1900`
  warnings because package vulnerability metadata could not be fetched from
  `https://api.nuget.org/v3/index.json` in the restricted environment.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed.

Result: passed.

## 2026-06-06 - RCA external fact API correlation

Scope: validate 3E, external module fact ingestion through the existing RCA fact API.

Checks:

- Added `ExternalSourceSystem`, `ExternalEventId` and `ExternalRecordUri` to RCA facts.
- Added idempotency for `POST /api/v1/rca/incidents/{id}/facts` when the same RCA receives the same external system/event pair.
- Extended fact DTO/request, EF and in-memory services, MVC form, fact line, unified timeline references and PDF export.
- Added EF migration `20260606112613_AddRcaFactExternalCorrelation`.
- Added regression coverage for external fact idempotency and incomplete correlation validation.
- Documented the external fact payload in `docs/API_CONTRACTS.md`.
- Build passed with 0 errors.
- EF reports no pending model changes after migration.

Database:

- Migration `20260606112613_AddRcaFactExternalCorrelation` applied successfully to local MySQL.

Result: passed.

## 2026-06-04 - RCA unified timeline and resolution policy

Scope: validate the unified investigation timeline and RCA resolution classification.

Checks:

- Added a unified timeline view model for incident detail events.
- Timeline now groups facts, evidence, corrective actions, wizard progress and external intake events with badges, references and industrial context.
- Corrective actions carry `ActionType` and `ResolutionScope` for root-cause and FUGA/no-detection resolution.
- Added resolution policy requiring recurrence prevention for root cause and a full corrective/preventive/recurrence set when FUGA is analyzed.
- Added EF migration `20260604143417_AddRcaResolutionActionClassification`.
- Added lightweight domain policy test executable.
- Build passed with 0 errors.
- EF reports no pending model changes after migration.

Database:

- Migration `20260604143417_AddRcaResolutionActionClassification` applied successfully to local MySQL.

Result: passed.

## 2026-06-04 - RCA manual fact line

Scope: validate 3A, the manual RCA fact line for investigation facts.

Checks:

- Added `RcaFact` domain entity, DTO/request contracts, EF mapping and migration `20260604135559_AddRcaFacts`.
- Added service methods to list and create facts.
- Added MVC detail panel and form for manual facts.
- Added API v1 endpoints `GET/POST /api/v1/rca/incidents/{id}/facts`.
- Added fact line content to RCA PDF export.
- Build passed with 0 errors.

Database:

- Migration generation passed.
- Applying the migration to local MySQL was blocked by local credentials: `Access denied for user 'ishikawa_user'@'localhost'`.

Result: code/build passed; database application pending local credential fix.

## 2026-06-04 - RCA fact linked records

Scope: validate 3B, facts linked to causes, evidence, corrective actions and external intake.

Checks:

- Added `CorrectiveActionId` to `RcaFact`, request/DTO contracts and service mappings.
- Added validation that linked corrective actions belong to the same RCA incident.
- Extended MVC fact form and fact list with action and external intake links.
- Extended PDF fact line with action and external intake references.
- Build passed with 0 errors.
- EF reports no pending model changes after migration.

Database:

- Migration `20260604140604_AddRcaFactActionLink` is a no-op generated before rebuilding; it was applied to local MySQL and preserved for migration history consistency.
- Migration `20260604140741_AddRcaFactCorrectiveActionId` added `CorrectiveActionId` and index `IX_rca_facts_TenantId_CorrectiveActionId`.
- Database update completed successfully using the local development connection string through `ISHIKAWA_RCA_CONNECTION`.

Result: passed.

## 2026-06-04 - RCA fact industrial classification

Scope: validate 3C, industrial classification for RCA facts.

Checks:

- Added fact severity, shift, machine, line, work order, material, lot, alarm and measurement fields to RCA facts.
- Extended DTO/request contracts, EF service, in-memory service, MVC form, detail display, event payload and PDF export.
- Added fact type options for customer claim, supplier claim and containment.
- Build passed with 0 errors.
- EF reports no pending model changes after migration.

Database:

- Migration `20260604141954_AddRcaFactIndustrialClassification` applied successfully.
- `FactSeverity` uses default `Info` for existing rows.

Result: passed.

## 2026-06-04 - RCA PDF export

Scope: validate PDF export with RCA closure and evidence manifest.

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://127.0.0.1:5075`
- Database-backed incident data

API/UI smoke:

- Created temporary closed RCA incident `2fc5ccb1-ab61-4ba2-8d8d-cc40ac5a7438` with root cause, validated evidence, completed corrective action and formal closure summary.
- Downloaded `GET /Rca/ExportPdf/{id}`.
- Confirmed `Content-Type: application/pdf`.
- Confirmed PDF header `%PDF-`.
- Confirmed generated file size `4329` bytes.
- Confirmed detail page renders `Exportar PDF` link.

Result: passed.

## 2026-06-04 - RCA guided wizard progress

Scope: validate the deeper guided RCA wizard with API progress, stronger prerequisites, and UI checklist.

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://127.0.0.1:5075`
- Database-backed incident data

API smoke:

- Created temporary RCA incident `6bb0a373-7f39-4899-84fb-9d91514a2eb8` with `SourceSystem=SMOKE`.
- Confirmed initial wizard progress: `CurrentStep=Problem`.
- Confirmed `POST /api/v1/rca/incidents/{id}/wizard/step` to `Actions` blocks without prerequisites.
- Added root cause, validated evidence, corrective action, and completed the action.
- Advanced wizard to `Validation`.
- Confirmed progress response: `CurrentStep=Validation`, `NextRecommendedStep=Closed`, `CompletionPercent=80`, and `Closed` blocked until formal RCA closure.

UI smoke:

- Detail page returned `200`.
- Wizard checklist rendered.
- Completion `80%` rendered.
- Closed-stage blocker rendered.
- Validated evidence metric rendered.

Result: passed.

## 2026-06-03 - RCA evidence thumbnail card

Scope: validate the RCA evidence card after adding image thumbnails and overflow protection.

Validated URL:

`http://127.0.0.1:5075/Rca/Details/20163d83-e46e-4cb1-846f-45bdeb7572b6`

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://localhost:5075`
- Database-backed incident data

Checks:

- Detail page returned successfully.
- Evidence card rendered.
- Image attachment thumbnail rendered through `PreviewEvidence`.
- Hash line rendered in compact mode.
- Overflow check returned `maxOverflow=0`, so no evidence card child escaped horizontally.

Result: passed.

## 2026-06-04 - RCA evidence validation metadata

Scope: validate stronger RCA evidence metadata for tags, detailed source, and validation status.

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://127.0.0.1:5075`
- Database-backed incident data
- EF migration applied: `20260603201406_AddRcaEvidenceValidationMetadata`

API smoke:

- Created temporary RCA incident `735571ff-1abc-4f92-9c32-4b95d61c2d10` with `SourceSystem=SMOKE`.
- Created evidence `9d4928c9-9cad-4eef-b2fc-cd47d2fa818f` with `SourceDetail`, duplicate tags, `Validated` status, validator, and validation notes.
- Updated the evidence through `PUT /api/v1/rca/incidents/{id}/evidence/{evidenceId}`.
- Confirmed response/list values: `ValidationStatus=Rejected`, `Tags=sensor, prensa-4, validado`, `SourceDetail=PLC prensa 4 / canal 02`, and `ValidatedAt` present.

UI smoke:

- Detail page returned `200`.
- Evidence card rendered `validation-chip-rejected`.
- Evidence card rendered tags `#sensor`, `#prensa-4`, and `#validado`.
- Evidence card rendered detailed source.
- Add/edit forms rendered `EvidenceForm.ValidationStatus` and `EvidenceEdit.ValidationStatus`.

Result: passed.

## 2026-06-03 - RCA evidence compact previews

Scope: validate compact attachment preview behavior for the RCA evidence card.

Validated URL:

`http://127.0.0.1:5075/Rca/Details/20163d83-e46e-4cb1-846f-45bdeb7572b6`

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://localhost:5075`
- Database-backed incident data

Checks:

- Detail page returned `200`.
- Evidence card rendered `data-preview-kind`.
- Evidence card rendered grouped file actions.
- Inline preview link rendered for previewable attachments.
- Existing image preview returned `200`, `Content-Type=image/jpeg`, and `Content-Disposition=inline`.
- UI classification now covers image, video, PDF, text/CSV/JSON/XML, Office, and generic file tiles.

Result: passed.

## 2026-06-03 - RCA evidence actions

Scope: validate evidence management actions for metadata edition, attachment replacement, and deletion.

Environment:

- `ASPNETCORE_ENVIRONMENT=Development`
- Local web host: `http://localhost:5075`
- Database-backed incident data

API smoke:

- Created temporary RCA incident with `SourceSystem=SMOKE`.
- Uploaded initial evidence attachment.
- Updated evidence metadata through `PUT /api/v1/rca/incidents/{id}/evidence/{evidenceId}`.
- Replaced evidence attachment through `POST /api/v1/rca/incidents/{id}/evidence/{evidenceId}/attachment`.
- Deleted evidence through `DELETE /api/v1/rca/incidents/{id}/evidence/{evidenceId}`.
- Confirmed evidence list returned `0` records after deletion.

UI smoke:

- Detail page returned `200`.
- Evidence management panel rendered.
- Update, replace attachment, and delete evidence forms rendered.

Result: passed.
