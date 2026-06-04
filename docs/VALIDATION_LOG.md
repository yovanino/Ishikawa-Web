# Validation Log

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
