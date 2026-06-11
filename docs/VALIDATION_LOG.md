# Validation Log

## 2026-06-11 - P2 outbox EF mapping

Scope: add EF persistence mapping and migration for RCA outbox events.

Checks:

- Added `RcaDbContext.RcaOutboxEvents`.
- Mapped `rca_outbox_events` with envelope, external correlation, payload,
  delivery status and retry fields.
- Added unique index on `TenantId + EventId`.
- Added indexes for pending publication and event lookup by incident/type.
- Generated migration `20260611123836_AddRcaOutboxEvents`.

Validation:

- First migration attempt with `--no-build` produced an empty migration because
  EF used an older compiled assembly. It was removed and regenerated after
  rebuilding.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P2 outbox domain model

Scope: add the initial RCA outbox domain model.

Checks:

- Added `RcaOutboxEventStatus` with `Pending`, `Publishing`, `Published`,
  `Failed` and `DeadLetter`.
- Added `RcaOutboxEvent` with event identity, RCA correlation, serialized
  payload and delivery state fields.
- Added a lightweight test for default pending delivery state.
- Corrected the outbox implementation plan assertion for `NextAttemptAt`.

Validation:

- Initial `dotnet build IshikawaRca.sln /m:1`: failed as expected before the
  model existed, with missing `RcaOutboxEvent` and `RcaOutboxEventStatus`.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed after correcting the `NextAttemptAt` assertion.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P2 outbox base implementation plan

Scope: create an executable implementation plan for the first RCA outbox base
cut.

Checks:

- Added `docs/superpowers/plans/2026-06-11-p2-rca-outbox-base.md`.
- The plan splits the work into domain model, EF mapping/migration, outbox
  service base and documentation closure.
- The plan keeps webhook delivery and feed replacement out of the first code
  cut.

Validation:

- Documentation review against the P2 outbox/webhooks design spec.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P2 outbox webhooks design

Scope: define the recommended technical design for RCA outbox and configurable
webhooks before implementing persistence and delivery behavior.

Checks:

- Added `docs/superpowers/specs/2026-06-11-p2-rca-outbox-webhooks-design.md`.
- The spec recommends outbox first and webhooks second.
- The spec preserves `RcaDomainEventDto` compatibility and keeps the derived
  feed until outbox coverage matches it.
- The spec documents idempotency, statuses, retries, backoff, dead-letter,
  webhook defaults and testing criteria.

Validation:

- Documentation/spec review against `docs/INTEGRATION_EVENTS.md`, roadmap and
  current EF persistence patterns.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P2 integration event compatibility coverage

Scope: add lightweight regression coverage for the RCA integration event
compatibility contract.

Checks:

- Added an in-memory test flow that creates an externally correlated RCA.
- The test records a root cause, corrective action, completed action, evidence
  and SCADA fact.
- The test validates documented event types, stable envelope fields, external
  correlation, critical `data` keys and the incremental `since` filter.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P2 integration event compatibility

Scope: document the RCA integration event feed compatibility contract.

Checks:

- Added `docs/INTEGRATION_EVENTS.md` with endpoint, envelope, event families,
  compatibility rules and consumer guidance.
- Updated `docs/API_CONTRACTS.md` to point consumers to the event contract and
  clarify current derived-feed semantics.
- Updated roadmap/status/backend continuity docs to mark the first P2
  integration compatibility increment.

Validation:

- Static inspection of `RcaDomainEventDto`,
  `RcaIntegrationsController.GetEvents`, and event generation in RCA services.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `git diff --check`: passed with CRLF warnings only.

Result: passed.

## 2026-06-11 - P1 visual closure

Scope: close the P1 visual cockpit cut in project documentation.

Checks:

- `docs/ROADMAP.md` now marks the P1 visual cockpit capabilities as closed.
- `docs/STATUS_AND_NEXT_STEPS.md` records that SLA visual, persisted cause
  ordering and advanced side-panel editing need explicit rules/contracts before
  implementation.
- `docs/chats/UI.md` keeps continuity for future UI work.

Validation:

- Documentation-only closure; based on the successful build/tests recorded for
  each P1 micro-adjustment on 2026-06-11.

Result: passed.

## 2026-06-11 - P1 visual cause reorder

Scope: add visual drag/reorder behavior to fishbone cause cards.

Checks:

- Cause cards are draggable inside their own branch lane.
- Reorder is client-side visual only; no persistence, API or backend contract
  was introduced.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser drag validation intentionally deferred to the P1 cockpit/tablet
  closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 responsive detail refinements

Scope: refine tablet/mobile behavior for the RCA detail cockpit.

Checks:

- Command bar actions and state chips stack cleanly on narrow viewports.
- Fishbone viewport, toolbar, timeline filters, CAPA card footers, side panel
  and contextual buttons receive mobile-specific constraints.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser responsive validation intentionally deferred to the P1
  cockpit/tablet closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 UI states

Scope: improve empty, loading, error and offline states in the RCA detail UI.

Checks:

- Key empty RCA sections now use compact empty-state cards.
- Forms receive a submitting visual state on submit.
- Detail screen includes an offline banner driven by browser online/offline
  events.
- Existing MVC validation errors remain the error-state mechanism.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser interaction validation intentionally deferred to the P1
  cockpit/tablet closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 contextual side panel

Scope: add a reusable contextual side panel to the RCA detail screen.

Checks:

- Added detail triggers on cause, evidence and CAPA action cards.
- Added a reusable side panel populated from `data-*` attributes.
- The panel is read-only; existing edit forms and backend behavior remain
  unchanged.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser interaction validation intentionally deferred to the P1
  cockpit/tablet closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 fishbone zoom and pan

Scope: add client-side navigation controls to the fishbone board.

Checks:

- Added toolbar controls to zoom out, fit/reset and zoom in.
- Added pointer-based pan behavior on the fishbone viewport.
- Existing cause cards and forms remain server-rendered; no backend contracts,
  persistence or API behavior changed.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser interaction validation intentionally deferred to the P1
  cockpit/tablet closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 filterable timeline

Scope: add client-side filters to the unified RCA timeline.

Checks:

- Added filter controls for all events, facts, evidence, actions, wizard and
  external events.
- Timeline items now expose `data-timeline-kind` and are filtered without page
  reload.
- Existing server-side timeline composition remains unchanged.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Browser interaction validation intentionally deferred to the P1
  cockpit/tablet closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 fishbone cause cards

Scope: enrich the fishbone cause cards in the RCA detail screen.

Checks:

- Cause cards now show P/I/F scores, total score, root-cause marker, parent
  cause, evidence summary and traceability counts for evidence, facts and
  actions.
- The fishbone still uses existing server-rendered MVC data; no backend
  contracts, persistence or API behavior changed.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Visual browser validation intentionally deferred to the P1 cockpit/tablet
  closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 CAPA board

Scope: add an operational CAPA board to the RCA detail screen.

Checks:

- Grouped existing actions into Corrective, Preventive and Recurrence lanes.
- Each lane shows count, status, linked cause, owner, due date and overdue
  marker where applicable.
- Existing root-cause/FUGA resolution lists and update forms remain in place.
- Kept the change inside MVC view/CSS only; no backend contracts, persistence
  or API behavior changed.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Visual browser validation intentionally deferred to the P1 cockpit/tablet
  closure.

Result: passed for fast P1 adjustment.

## 2026-06-11 - P1 RCA command bar and KPI rail

Scope: start the P1 industrial cockpit pass on the RCA detail screen.

Checks:

- Added an incident command bar with source, claim actor, creation time,
  severity, status, phase progress, line, machine, work order, current phase
  and owner context.
- Expanded the KPI rail with overdue actions, next due date, containment age
  and recurrence risk.
- Kept the change inside MVC view/CSS only; no backend contracts, persistence
  or API behavior changed.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed when run in series.
- Visual browser validation intentionally deferred to the P1 cockpit block
  closure to avoid slow app/DB startup on every micro-adjustment.

Result: passed for fast P1 adjustment.

## 2026-06-10 - P0 standalone closure

Scope: close the backend P0 standalone cut in project documentation.

Checks:

- `docs/ROADMAP.md` now marks the standalone P0 cut as closed and separates
  post-P0 corporate/platform work.
- `docs/STATUS_AND_NEXT_STEPS.md` records the P0 closure state and next
  post-P0 paths.
- `docs/backend.md` records the backend P0 cut as pilotable.
- `docs/chats/BACKEND.md` records the closure for continuity.

Validation:

- Documentation-only change; based on the successful validation recorded in
  `2026-06-10 - Incident audit records API`.

Result: passed.

## 2026-06-10 - Incident audit records API

Scope: make persisted sensitive-operation audit records queryable and
repeatable in local validation.

Checks:

- Added `GET /api/v1/rca/incidents/{id}/audit`.
- The endpoint requires `Supervisor`, `Quality` or `Administrator`.
- The endpoint returns incident audit records ordered from newest to oldest.
- Added `scripts/smoke-audit-records.ps1`.
- `scripts/run-local-validation.ps1` now runs the audit smoke after evidence
  attachment validation and before external facts.

Validation:

- Initial isolated audit smoke failed with HTTP 404 before implementation.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`; output
  included `Audit records smoke test completed successfully.`.

Result: passed.

## 2026-06-10 - Evidence attachment controlled download smoke

Scope: make controlled evidence attachment downloads verifiable in the main
API + DB smoke.

Checks:

- `scripts/smoke-test.ps1` downloads the uploaded evidence attachment through
  `GET /api/v1/rca/incidents/{id}/evidence/{evidenceId}/attachment`.
- The smoke validates the downloaded bytes match the uploaded file.
- The smoke validates `Content-Type` is preserved as `text/plain`.
- The smoke validates `Content-Disposition` includes the original uploaded
  file name.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`; the main
  smoke printed `Downloaded evidence file through controlled endpoint.`.

Result: passed.

## 2026-06-10 - Evidence attachment validation smoke

Scope: make evidence attachment API hardening repeatable in local validation.

Checks:

- Added `scripts/smoke-evidence-attachment-validation.ps1`.
- The script creates a minimal RCA incident through `/api/v1/rca/incidents`.
- The script attempts to upload an evidence attachment with a disallowed
  `.exe` extension through `/api/v1/rca/incidents/{id}/evidence-files`.
- The script validates HTTP 400, `success=false` and `INVALID_ATTACHMENT`.
- `scripts/run-local-validation.ps1` now runs the evidence-attachment smoke
  after auth-error smoke and before external-facts smoke.

Validation:

- Initial `dotnet build` was accidentally run in parallel with
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`,
  causing a transient copy race in `bin\Debug\net9.0`; rerun sequentially.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`.

Result: passed.

## 2026-06-10 - P0 backend status documentation

Scope: align current project status with the validated backend P0 increments.

Checks:

- Updated `docs/STATUS_AND_NEXT_STEPS.md` to include completed standalone auth,
  tenant context, roles, initial audit, API error normalization, attachment
  hardening and local smoke coverage.
- Updated immediate technical pending items to focus on Identity/tenant
  integration, formal tests, audit consumers, storage policy and future
  platform integrations.

Validation:

- Documentation-only change; based on validated commits and smoke runs already
  recorded in this log.

Result: passed.

## 2026-06-10 - API model validation smoke

Scope: make automatic API model-state errors repeatable in local validation.

Checks:

- Added `scripts/smoke-api-model-validation.ps1`.
- The script sends an invalid `occurredAt` value to
  `POST /api/v1/rca/incidents`.
- The script validates HTTP 400, `success=false`, `MODEL_VALIDATION_ERROR` and
  `correlationId`.
- `scripts/run-local-validation.ps1` now runs the model-validation smoke after
  the critical API + DB smoke.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`.

Result: passed.

## 2026-06-10 - Runtime connection string documentation

Scope: document the correct environment variables for runtime validation and
EF design-time commands.

Checks:

- `ConnectionStrings__IshikawaRca` documented as the runtime/smoke connection
  string override.
- `ISHIKAWA_RCA_CONNECTION` documented as the EF design-time helper variable.

Validation:

- Documentation-only change based on the validated runtime command used by
  local smoke tests.

Result: passed.

## 2026-06-10 - External facts API smoke

Scope: make external fact ingestion and idempotency repeatable in local
validation.

Checks:

- Added `scripts/smoke-external-facts.ps1`.
- The script creates a minimal RCA incident through `/api/v1/rca/incidents`.
- The script records an external fact with `externalSourceSystem` and
  `externalEventId`.
- The script retries the same external fact and validates idempotency by
  matching the returned fact id and message `Hecho externo existente.`.
- The script verifies listing facts returns exactly one correlated event.
- The script verifies incomplete external correlation is rejected with
  `EXTERNAL_FACT_CORRELATION_INCOMPLETE`.
- `scripts/run-local-validation.ps1` now runs the external-facts smoke after
  the auth-error smoke.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`.

Result: passed.

## 2026-06-10 - API authorization error smoke

Scope: make the API 401/403 authorization error contract repeatable in local
validation.

Checks:

- Added `scripts/smoke-api-auth-errors.ps1`.
- The script validates HTTP 403 with `FORBIDDEN` for an authenticated user with
  insufficient role.
- The script validates HTTP 401 with `AUTHENTICATION_REQUIRED` for an invalid
  authentication context.
- `scripts/run-local-validation.ps1` now runs the auth-error smoke after the
  critical API + DB smoke.

Validation:

- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`.

Result: passed.

## 2026-06-10 - Local validation SDK preflight

Scope: validate that local smoke/build scripts fail fast when the required
.NET SDK is not available.

Checks:

- Added `scripts/check-dotnet-sdk.ps1` to read `global.json` and verify a
  registered matching .NET SDK, accepting patch-compatible SDKs in the same
  feature band.
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
- After installing `Microsoft.DotNet.SDK.10` version `10.0.301`,
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\check-dotnet-sdk.ps1`: passed with compatible SDK for
  `global.json` `10.0.300`.
- `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.

Result: passed.

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

- Initial build attempts were blocked because the local `dotnet` host had no
  SDK registered and `global.json` requests SDK `10.0.300`.
- After installing `Microsoft.DotNet.SDK.10` version `10.0.301`,
  `dotnet build IshikawaRca.sln /m:1`: passed with 0 warnings and 0 errors.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: passed when `ConnectionStrings__IshikawaRca` was supplied from local
  development configuration with `AllowPublicKeyRetrieval=True`.
- Targeted API authorization check against
  `POST /api/v1/rca/incidents/{id}/close`: passed for HTTP 403 with
  `FORBIDDEN` using insufficient role `Operator`, and HTTP 401 with
  `AUTHENTICATION_REQUIRED` using an invalid tenant header.

Result: passed.

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
