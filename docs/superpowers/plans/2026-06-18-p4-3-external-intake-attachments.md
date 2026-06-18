# P4.3 External Intake Attachments Plan

## Goal

Implement controlled binary attachments for customer/supplier intake in a later
cut, preserving the current token-limited external surface.

## Tasks

- [ ] Add external intake attachment domain model with tenant, intake id,
  metadata, storage provider/key, SHA-256, status and review fields.
- [ ] Add EF mapping and migration with indexes by tenant/intake/status.
- [ ] Add token-scoped upload endpoint on the external intake flow.
- [ ] Add internal review UI/API to accept or reject uploaded attachments.
- [ ] When accepted, optionally create official `RcaEvidence` linked to the
  external intake.
- [ ] Add audit records for upload, accept/import and reject.
- [ ] Add smoke/test coverage for expired token, revoked token, unsafe file and
  successful import.

## Validation

Run in series:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Any local web smoke must use startup/request/shutdown timeouts and confirm no
`dotnet run` process remains alive.
