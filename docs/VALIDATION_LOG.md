# Validation Log

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
