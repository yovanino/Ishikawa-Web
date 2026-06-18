# P4.4 Platform Contracts Plan

## Goal

Add platform-readiness contracts only after the global app, Identity provider
or master-data services define a concrete consumer.

## Tasks

- [ ] Add optional identity reference fields only when the external identity
  contract is known.
- [ ] Add optional customer/supplier master ids while preserving denormalized
  names for standalone mode.
- [ ] Add a read-only module capabilities endpoint if the app global needs
  discovery.
- [ ] Extend integration snapshots with closure document state and dashboard
  counters as additive fields.
- [ ] Confirm unified timeline consumers use `/events`, `/events/live`,
  outbox/webhooks or snapshots rather than internal DB tables.
- [ ] Document compatibility in `docs/API_CONTRACTS.md` and
  `docs/INTEGRATION_EVENTS.md` before any endpoint change.

## Validation

Run in series:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Any local integration smoke must use explicit timeouts and clean up all local
processes.
