# External Claim Intake

Date: 2026-05-29

Scope: supplier/customer intake concept for the Ishikawa RCA module inside the future industrial operations platform.

## Strategic Direction

Ishikawa RCA must remain an independent module, but it should be ready to participate in a larger industrial platform with shared identity, suppliers, customers, events, AI, rules and audit.

External claim intake should not become a disconnected form. It should become a controlled entry point into the RCA workflow.

## Claim Actor Model

The current MVP separates:

- `ClaimScope = Internal`: the claim comes from an internal area.
- `ClaimScope = External`: the claim comes from outside the plant organization.
- `ClaimActorType = InternalArea | Customer | Supplier`: the actor that originated or owns the claim.
- `ClaimOwnerName`: area, customer or supplier name depending on context.

For the larger platform, this should evolve into a master-data-linked model:

- `ClaimActorId`: optional reference to the global master data record.
- `ClaimActorName`: denormalized display name captured at the time of RCA creation.

This keeps the RCA module standalone while allowing later linkage to global master data.

## Supplier Link Concept

Yes, suppliers can be included through a secure intake link.

Recommended flow:

1. Internal user creates or assigns a supplier-related RCA.
2. System generates a secure external intake link.
3. Supplier opens the link without full platform access.
4. Supplier completes only the allowed claim fields:
   - contact name
   - supplier claim reference
   - affected material or batch
   - description
   - containment response
   - evidence attachments
   - proposed corrective action
5. Internal quality/supply chain user reviews and imports/approves the supplier input.
6. Approved data becomes part of the RCA audit trail.

MVP implemented:

- Internal user can generate supplier/customer links from the RCA detail screen.
- Token is shown once and stored as a SHA-256 hash.
- External page has no full module navigation.
- External actor can submit reference, material, lot, description, containment, proposed root cause, proposed action and evidence summary.
- Internal screen shows intake status and submitted response.
- Internal reviewer can accept a submitted response, import the proposed root cause into an Ishikawa branch, optionally mark it as root cause and import the proposed corrective action.
- Integration event feed exposes created, opened, submitted, reviewed, revoked and expired intake states for the future global platform.
- Links can be revoked before submission/review.

## Security Rules

External links must be treated as controlled access tokens, not public pages.

Minimum rules:

- Token with expiration.
- Token bound to one RCA or one intake request.
- Read/write scope limited to supplier/customer fields.
- No access to other incidents.
- No internal comments unless explicitly shared.
- Attachment size/type restrictions.
- Audit every external update.
- Optional one-time password or email verification.
- Revocation from the internal RCA screen.

## Platform Boundary

The RCA module should own:

- Intake request state.
- RCA-specific supplier/customer responses.
- Mapping supplier/customer response into causes, evidence and actions.
- Audit trail of imported external input.

The future global platform should own:

- Supplier master data.
- Customer master data.
- Identity and external access policy.
- Notification delivery.
- Global document storage policy.
- Cross-module audit and governance.

During MVP, the RCA module can store denormalized actor fields and token records locally. When the global platform exists, those become integration references.

## Data Needed Later

Future entities:

- `RcaExternalIntakeRequest`
  - `Id`
  - `RcaIncidentId`
  - `ActorType`
  - `ActorId`
  - `ActorName`
  - `ContactEmail`
  - `TokenHash`
  - `ExpiresAt`
  - `Status`
  - `SubmittedAt`
  - `ReviewedAt`
  - `ReviewedByUserId`

- `RcaExternalIntakeResponse`
  - `IntakeRequestId`
  - `ClaimReference`
  - `MaterialCode`
  - `BatchOrLot`
  - `Description`
  - `ContainmentResponse`
  - `ProposedRootCause`
  - `ProposedCorrectiveAction`
  - `EvidenceSummary`

## UX Direction

Internal RCA screen:

- Add claim actor card: internal area, customer or supplier.
- Add button: generate external link.
- Show link status: draft, sent, opened, submitted, reviewed, expired, revoked.
- Show external response as pending evidence until accepted.

External supplier/customer screen:

- No full navigation.
- Branded minimal intake page.
- Clear incident reference and requested input.
- Attachment/evidence upload.
- Submit and confirmation.
- No visibility into internal investigation unless shared.

## AI Opportunities

AI Gateway can help by:

- Summarizing supplier responses.
- Detecting missing fields before submission.
- Comparing supplier explanation with internal evidence.
- Suggesting causes or CAPA, marked as AI-assisted.
- Flagging inconsistent supplier/customer claims.

AI must not auto-accept external responses into the official RCA. A human reviewer approves.

## Immediate Recommendation

Next data-model step: harden formal review/import:

- reviewer can reject external response with reason;
- approved response can become a dedicated evidence record;
- proposed corrective action can become CAPA draft with owner, due date and validation gate;
- attachments can be added with storage policy from the global platform.
