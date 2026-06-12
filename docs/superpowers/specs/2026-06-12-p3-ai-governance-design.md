# P3 AI Gateway and Human Approval Design

Date: 2026-06-12.

## Goal

Close P3 by moving Ishikawa RCA from deterministic AI stub only to governed AI
assistance that can call a real AI Gateway, keep operating when AI is disabled
or degraded, and ensure no AI suggestion becomes an official RCA decision
without human approval and auditability.

## Scope

P3 includes:

- HTTP client implementation for the shared AI Gateway.
- Environment-level AI Gateway configuration.
- AI response metadata for provider, model, fallback mode and confidence when
  available.
- New AI assistance capabilities for recurrence detection and 8D draft.
- Persistent AI suggestion records.
- Accept/reject workflow for suggestions.
- Audit trail for accepted/rejected AI suggestions.
- MVC UI surface for reviewing suggestions before applying them.

P3 excludes:

- Embedding a local model inside Ishikawa RCA.
- Direct dependency on a specific AI engine such as Ollama, vLLM, OpenAI or
  another vendor.
- Letting AI close RCA, select root causes or create official actions without a
  human accept operation.
- Tenant-specific AI policy beyond configuration shape. Tenant-level policy can
  be added later when corporate Identity/tenant resolution exists.

## Architecture

The existing `IRcaAiGatewayClient` remains the application boundary. The
infrastructure layer will provide:

- `StubRcaAiGatewayClient` for deterministic local development and fallback.
- `HttpRcaAiGatewayClient` for `AiGateway:Mode = Http`.
- Optional wrapper/factory behavior that chooses stub or HTTP from
  configuration without changing controllers or application services.

The RCA module talks to the AI Gateway using HTTP JSON contracts. The gateway is
responsible for model/provider selection. Ishikawa RCA stores returned metadata
but does not know or depend on the engine implementation.

## Configuration

Existing `AiGateway` configuration is extended but remains backward compatible:

```json
{
  "AiGateway": {
    "Mode": "Stub",
    "BaseUrl": "",
    "TimeoutSeconds": 30,
    "ApiKey": "",
    "UseFallbackOnFailure": true
  }
}
```

Rules:

- `Mode = Stub` uses deterministic local responses.
- `Mode = Http` requires `BaseUrl`.
- `ApiKey` is optional and, when present, is sent as a bearer token.
- `UseFallbackOnFailure = true` returns deterministic fallback suggestions with
  `isFallback = true`.
- `UseFallbackOnFailure = false` returns a controlled API error and does not
  block normal RCA operation.

No secrets are committed to source control. Local secret values belong in
environment variables or local untracked configuration.

## AI Capabilities

Existing public module endpoints remain:

```http
POST /api/v1/rca/incidents/{id}/ai/suggest-causes
POST /api/v1/rca/incidents/{id}/ai/suggest-actions
POST /api/v1/rca/incidents/{id}/ai/summarize
```

P3 adds:

```http
POST /api/v1/rca/incidents/{id}/ai/detect-recurrence
POST /api/v1/rca/incidents/{id}/ai/generate-8d-draft
```

All endpoints build context from the existing RCA application service before
calling the gateway. They return suggestions only. They do not mutate official
RCA entities directly.

## Gateway Contracts

The module calls the gateway endpoints documented in `docs/AI_INTEGRATION.md`:

```http
POST /ai/rca/suggest-causes
POST /ai/rca/suggest-actions
POST /ai/rca/summarize
POST /ai/rca/detect-recurrence
POST /ai/rca/generate-8d-draft
```

Requests contain the RCA context DTO already assembled by the application
layer. Responses are mapped into module contracts and include metadata:

- `provider`
- `model`
- `isFallback`
- `confidence`
- `generatedAt`
- `correlationId` when supplied by the gateway

Unknown additive response fields from the gateway are ignored.

## Suggestion Persistence

P3 introduces persistent AI suggestion records, tentatively
`RcaAiSuggestion`.

Fields:

- `Id`
- `TenantId`
- `IncidentId`
- `SuggestionType`: `Cause`, `Action`, `Summary`, `Recurrence`, `EightD`
- `Status`: `Pending`, `Accepted`, `Rejected`, `Expired`
- `Title`
- `Summary`
- `PayloadJson`
- `Provider`
- `Model`
- `IsFallback`
- `Confidence`
- `GatewayCorrelationId`
- `CreatedAt`
- `CreatedByUserId`
- `ReviewedAt`
- `ReviewedByUserId`
- `ReviewNotes`
- `AppliedEntityType`
- `AppliedEntityId`

The payload stores the original suggestion details for auditability and future
re-rendering. Official RCA entities continue to live in their existing tables.

## Human Approval Workflow

AI assistance follows this lifecycle:

1. User requests an AI suggestion.
2. Module builds RCA context and calls stub or HTTP gateway.
3. Module returns suggestions and persists them as `Pending`.
4. User reviews a suggestion in the RCA detail UI.
5. User accepts or rejects the suggestion.
6. Accepting applies a controlled mutation only for supported suggestion types:
   cause suggestion can create a cause, action suggestion can create an action,
   summary can populate a draft/notes field when available, recurrence and 8D
   remain documented draft outputs unless a later workflow defines official
   fields.
7. Accept/reject writes audit records.

The first P3 implementation should support accepting causes and actions because
the module already has official entities for them. Summary, recurrence and 8D
draft suggestions should initially be reviewable and auditable without
automatically modifying closure or escalation state.

## API Additions

Suggestion governance endpoints:

```http
GET  /api/v1/rca/incidents/{id}/ai/suggestions?status=
POST /api/v1/rca/incidents/{id}/ai/suggestions/{suggestionId}/accept
POST /api/v1/rca/incidents/{id}/ai/suggestions/{suggestionId}/reject
```

Accept request:

```json
{
  "reviewedByUserId": "calidad",
  "reviewNotes": "Aceptada tras revision con evidencia de linea.",
  "targetBranchId": "00000000-0000-0000-0000-000000000000"
}
```

Reject request:

```json
{
  "reviewedByUserId": "calidad",
  "reviewNotes": "No coincide con la evidencia validada."
}
```

Authorization follows the existing sensitive-operation roles:
`Supervisor`, `Quality` or `Administrator`.

## UI

The RCA detail page gets an AI assistance panel:

- Request buttons for causes, actions, summary, recurrence and 8D draft.
- Pending suggestion list with provider/model/fallback metadata.
- Accept/reject controls.
- Clear visual state for fallback results and gateway failures.
- No automatic write to official RCA data when a suggestion is generated.

The UI should remain compact and operational. It should not explain basic AI
concepts in-page; it should show the suggestion, provenance and review action.

## Error Handling

- Missing incident returns the existing not-found API pattern.
- Disabled/stub mode remains operational.
- HTTP timeout, DNS, gateway 5xx or invalid JSON produce either fallback
  suggestions or a controlled `AI_GATEWAY_UNAVAILABLE` response based on
  configuration.
- Accepting a non-pending suggestion returns `AI_SUGGESTION_NOT_PENDING`.
- Accepting a suggestion for a different incident returns not found.
- Accepting a cause suggestion without a target branch returns
  `AI_SUGGESTION_BRANCH_REQUIRED`.

## Testing

Implementation must use TDD for each behavioral increment:

- HTTP client maps request, headers, timeout and response metadata.
- HTTP client falls back or returns controlled failure on gateway errors.
- New recurrence and 8D endpoints return suggestions without mutating RCA.
- Suggestion persistence defaults to `Pending`.
- Accepting cause/action suggestions creates official entities and records
  audit.
- Rejecting suggestions records audit and does not create official entities.
- UI/controller smoke or lightweight tests verify endpoints and view model
  wiring where practical.

Validation for each commit:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

## Rollout

Suggested implementation order:

1. Add AI Gateway options/factory and HTTP client.
2. Add recurrence and 8D contracts/endpoints.
3. Add AI suggestion domain model, EF mapping and migration.
4. Persist generated suggestions as pending.
5. Add accept/reject service and API endpoints.
6. Add UI review panel.
7. Update docs and close P3 roadmap.

Each step should be committed separately after validation.

## Risks

- Gateway contract drift can break HTTP mode; mapping must tolerate additive
  fields and fail safely on invalid required fields.
- Without a real tenant policy provider, tenant-specific AI enablement should
  remain a documented future extension.
- UI could imply that AI decisions are official; labels and workflow must make
  human review the only path to official mutations.
- Persisting full prompts or sensitive payloads can expose data. P3 stores
  structured suggestion payloads, not raw hidden prompts, unless later policy
  explicitly requires prompt retention.
