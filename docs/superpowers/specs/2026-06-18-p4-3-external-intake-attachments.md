# P4.3 External Intake Attachments Spec

Date: 2026-06-18.

## Goal

Allow customer/supplier intake actors to upload scoped attachments without
granting access to the full RCA module and without importing those files into
official RCA evidence until an internal reviewer approves them.

## Scope

- Token-scoped upload tied to one external intake request.
- File type and size policy aligned with RCA evidence attachments.
- SHA-256 and storage metadata captured for every uploaded file.
- Internal review can import selected attachments as official RCA evidence.
- Audit records are written for upload, review/import and rejection.

## Security Rules

- Token must be valid, unexpired and not revoked.
- Upload does not expose any other RCA data.
- Attachments remain pending external input until reviewed.
- Internal reviewer identity comes from authenticated context.
- Storage keys are never exposed as filesystem paths.

## Out Of Scope

- Global supplier/customer portal.
- Notification delivery.
- External identity verification beyond the current token flow.
- Direct linkage to corporate master data tables.

## Acceptance

- External actors can attach binary context to one intake response.
- Rejected attachments remain traceable but do not become official evidence.
- Approved attachments can create RCA evidence records linked to the intake.
- The module remains standalone when no corporate document store exists.
