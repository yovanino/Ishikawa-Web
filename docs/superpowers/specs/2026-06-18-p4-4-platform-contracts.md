# P4.4 Platform Contracts Spec

Date: 2026-06-18.

## Goal

Prepare Ishikawa RCA for the future industrial platform through versioned
contracts, not direct references to external module tables or SDKs.

## Contract Areas

- Identity mapping: authenticated user id, roles, tenant and future external
  subject references.
- Customer/supplier masters: denormalized display names plus optional future
  master ids.
- App global registration: module name, base routes, health/readiness and
  capability flags.
- Dashboard summaries: read-only RCA status, severity, age, open actions,
  overdue actions, closure document status and recurrence risk.
- Unified timeline: consume existing integration events, SSE and outbox/webhook
  delivery without reading RCA tables directly.

## Rules

- Keep `/api/v1` backward compatible.
- Add new fields additively.
- Unknown events or `data` keys must be ignored by consumers.
- Cross-module dashboards must consume APIs/events, not EF entities.
- Identity and master data references are optional until corporate providers
  exist.

## Out Of Scope

- Implementing the global app shell.
- Implementing a corporate Identity provider.
- Implementing customer/supplier master services.
- Building a cross-module dashboard inside this repository.

## Acceptance

- The module can advertise capabilities and expose summary data without direct
  coupling.
- Current standalone auth/tenant behavior still works.
- Future platform integrations can be added as adapters around these contracts.
