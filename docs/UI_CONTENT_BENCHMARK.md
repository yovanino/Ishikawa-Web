# UI and Content Benchmark

Date: 2026-05-29

Scope: Ishikawa RCA standalone module, with future integration into the industrial operations platform.

## Executive Positioning

The product should feel less like a classic web form and more like an industrial command cockpit for problem solving.

The mainstream market is moving toward:

- Real-time operational context instead of static records.
- Role-based command surfaces for supervisors, quality, maintenance, production and leadership.
- Visual management boards that combine KPIs, events, causes, actions and escalation status.
- Guided problem-solving workflows: RCA, 5 Whys, A3, CAPA, DMAIC, PDSA.
- Human-in-the-loop AI: assistants can suggest, classify and summarize, but users approve important steps.
- Browser/tablet-first industrial UX: dense, readable, touch-friendly and reliable.

Our current UI redesign is a good first visual base. The next leap is content intelligence, interactivity and operational context.

## Reference Benchmark

| Reference | What it proves | What we should borrow |
| --- | --- | --- |
| [Tulip Frontline Operations Platform](https://tulip.co/) | Modern frontline apps combine production tracking, quality, traceability, dashboards and human approvals. | Modular app feel, operator-first workflows, AI/human approval boundaries, action-oriented dashboards. |
| [Siemens Opcenter Intelligence](https://www.siemens.com/en-us/products/opcenter/manufacturing-intelligence/) | Manufacturing intelligence is built around near-real-time plant, line and machine performance from multiple data sources. | Live operational context, multi-source data model, cross-module KPI cards. |
| [AVEVA PI Vision](https://www.aveva.com/en/products/aveva-pi-vision/) | Operations dashboards need trends, alarms, KPIs and contextual process data accessible from browser/tablet. | Trend panels, alarm/event strips, timeline overlays, status-first visual hierarchy. |
| [PTC ThingWorx Dashboards](https://support.ptc.com/help/thingworx/platform/r9/en/ThingWorx/Help/Composer/Visualization/Dashboards/Dashboards.html) | Supervisors monitor multiple lines through configurable dashboards. | Saved views, dashboard composition, role-specific layouts. |
| [Rockwell FactoryTalk Optix](https://www.rockwellautomation.com/en-us/products/software/factorytalk/optix.html) | Industrial HMI is moving to modern, scalable, interoperable web clients. | Touch-friendly components, industrial status states, robust disconnection/reconnect states. |
| [GE Vernova Proficy Smart Factory MES](https://www.gevernova.com/software/products/manufacturing-execution-systems) | MES value is performance management across mixed manufacturing operations. | Link RCA to performance loss, quality events, work orders and production context. |
| [Minitab Workspace](https://www.minitab.com/en-us/products/workspace/) | RCA tools win when they provide many standardized visual problem-solving templates. | Fishbone plus 5 Whys, A3, FMEA, process maps and standard templates. |
| [KaiNexus Problem Solving](https://www.kainexus.com/problem-solving-and-process-improvement-software) | Improvement platforms combine methodology, collaboration, progress tracking and impact measurement. | Guided RCA workflows, impact fields, progress status, sustainment checks. |
| [iObeya Digital Visual Management](https://www.iobeya.com/) | Lean users expect a visual room experience, not only database screens. | Obeya-style board, sticky-note cause mapping, action boards, daily management view. |
| [Microsoft Dynamics 365 Production Floor Execution](https://learn.microsoft.com/en-us/dynamics365/supply-chain/production-control/production-floor-execution-setup) | Production floor UI must support configured devices and shop-floor roles. | Tablet mode, station/device context, operator-safe interaction model. |

## Fishbone Creator Benchmark

Source: [Boardmix comparison of fishbone diagram creators](https://boardmix.com/reviews/fishbone-diagram-creator/).

The fishbone-specific market focuses mostly on diagram creation, templates, collaboration and export. This is useful, but it is not enough for an industrial RCA module. Our differentiator should be turning the fishbone from a drawing into an operational investigation object with evidence, owners, actions, status, audit trail and integrations.

### Selection Criteria Found In The Market

The mainstream criteria for a fishbone creator are:

- Accessibility across devices.
- Real-time collaboration.
- Integration with productivity or project tools.
- Ease of use.
- Ready-made templates.
- Export/sharing.

For Ishikawa RCA, these criteria are necessary but incomplete. We also need:

- Plant, line, asset, station and shift context.
- Evidence linked to causes.
- Cause confidence and validation state.
- Corrective/preventive action ownership.
- RCA phase and closure gates.
- Integration events for Gantt, OEE, Andon, TPM, SCADA/MES/MRP and AI Gateway.
- Auditability for operational and quality review.

### Tool Pros And Cons

| Tool | Pros to learn from | Cons / limits for our target | Implication for Ishikawa RCA |
| --- | --- | --- | --- |
| [Boardmix](https://boardmix.com/) | Real-time collaboration, intuitive interface, fishbone templates, free tier, multi-device use, Google Workspace integration. | Free tier is limited; positioned as a collaborative whiteboard more than an industrial RCA system. | Borrow collaborative canvas, template flow and low-friction creation. Add industrial records, evidence, actions and governance. |
| [SmartDraw](https://www.smartdraw.com/) | Professional templates, broad shapes/tools, integrations with Office, Atlassian and Google Workspace, multi-device use, support. | More expensive; customization can become complex. | Borrow template polish and enterprise diagram quality. Avoid heavy configuration for shop-floor users. |
| [DesignCap](https://www.designcap.com/) | Beginner-friendly, simple interface, many templates/images, affordable/free entry. | Limited free version and fewer customization options than stronger diagramming tools. | Borrow fast onboarding and simple creation. Avoid making RCA feel like a generic graphic design tool. |
| [Lucidchart](https://www.lucidchart.com/) | Strong collaboration, project tracking, many templates, automated data linking. | Subscription-gated features/storage; online-only. | Borrow data-linked diagrams and collaboration. Add offline/degraded industrial states and local deployment readiness. |
| [Creately](https://creately.com/) | Industry templates, integrations, team collaboration, task assignment and exports. | Not fully free; subscription cancellation friction. | Borrow task-follow-up directly from diagram nodes. Go further by making actions first-class CAPA records. |
| [Canva](https://www.canva.com/) | Huge visual/template library, easy customization, beginner-friendly collaboration. | Watermarks/free limits; possible slowdowns; design-first instead of investigation-first. | Borrow visual clarity and approachability. Avoid decorative graphics that reduce operational precision. |

### Competitive Gap We Can Own

Most creators help teams draw a fishbone. Our module should help teams close an industrial problem.

The winning direction is:

- Fishbone as a living RCA artifact, not a static diagram.
- Cause cards with owner, evidence, confidence and validation state.
- AI-assisted cause suggestions with human approval.
- CAPA actions generated from validated causes.
- Timeline and audit trail tied to every investigation change.
- Cross-module context from OEE, Andon, TPM, Gantt and SCADA events.
- Large-screen Obeya/team review mode plus dense supervisor cockpit.

### UI Lessons From Fishbone Creators

- Creation must be nearly instant: choose template, define problem, start adding causes.
- Branch editing must feel direct: add, rename, reorder, collapse and expand without page reloads.
- Collaboration cues matter: who edited, who owns, what changed, what is blocked.
- Export still matters: PDF/image/report for meetings and audits.
- The canvas needs zoom, fit-to-screen, print-friendly layout and tablet-friendly controls.
- Templates should not be visual-only; they should encode RCA methods and required fields.

## Graphic Direction

### 1. Industrial Premium, Not Marketing Premium

The UI should be premium through clarity, precision and operational density. Avoid large decorative hero areas inside working screens. Use:

- Command header with incident identity, severity, SLA, line, asset and live state.
- Compact KPI rail: open causes, overdue actions, containment age, recurrence risk.
- Dense table surfaces with strong scan patterns.
- Subtle depth, crisp borders, minimal gradients, clear status colors.
- Typography that feels enterprise-grade, not playful.

### 2. Cockpit Structure

Target working screen layout:

- Top: incident command bar.
- Left: context and evidence rail.
- Center: interactive fishbone or investigation board.
- Right: AI assistant, suggested causes, validations and next actions.
- Bottom: timeline, events, CAPA and audit trail.

### 3. Industrial Status Language

Define a shared visual grammar:

- Red: unsafe, stopped, overdue, critical recurrence.
- Amber: at risk, pending validation, containment active.
- Blue/cyan: informational, AI suggestion, external integration.
- Green: verified, closed, effective.
- Gray: draft, unavailable, offline, historical.

This grammar must be reusable by future modules: OEE Live, Andon, TPM, Heijunka, Yamazumi, VSM and Gantt.

## Content Direction

### What RCA Screens Need To Contain

Mainstream RCA tools are not only fishbone diagrams. They guide decisions. Add content blocks for:

- Problem statement: what, where, when, magnitude, impact.
- Containment: immediate action, owner, due date, effectiveness.
- Evidence: photos, sensor events, alarms, downtime, defects, documents.
- Cause exploration: 6M categories, 5 Whys chain, confidence, validation status.
- Root cause decision: selected root cause, rationale, rejected hypotheses.
- CAPA: corrective action, preventive action, owner, due date, verification.
- Effectiveness check: recurrence window, metric to monitor, closure gate.
- Lessons learned: standard update, training, checklist, maintenance plan.

### Content Missing Today

Current module direction is solid, but still needs:

- Timeline of incident evolution.
- Evidence attachment model.
- Cause confidence and validation workflow.
- Corrective/preventive action split.
- Approval/closure gates.
- Role-specific views.
- AI-generated summaries with explicit user approval.
- Integration placeholders for Gantt, Andon, OEE and maintenance/work order systems.

## Recommended Product Improvements

### P0 - Next Premium Pass

1. Add an incident command center details screen:
   - Severity, SLA, containment age, line/asset, owner, shift, current phase.
   - One-click state transitions: draft, containment, investigation, action, verification, closed.

2. Upgrade fishbone to an interactive board:
   - Cause cards per 6M branch.
   - Confidence, evidence count, validation state and AI suggestion marker.
   - Drag/reorder inside branch.

3. Add RCA timeline:
   - Incident created, cause added, action assigned, AI suggestion, status change.
   - Future integration point for Andon/OEE/SCADA events.

4. Add CAPA board:
   - Corrective vs preventive actions.
   - Owner, due date, overdue marker, verification result.

5. Add empty/loading/error/offline states:
   - Needed for premium perception and industrial reliability.

### P1 - Content and Workflow Depth

1. Add guided RCA templates:
   - 6M Fishbone.
   - 5 Whys.
   - A3.
   - 8D-lite.

2. Add evidence model:
   - Files, notes, process tags, alarms, downtime windows, quality checks.

3. Add AI assistant panel:
   - Suggest causes.
   - Detect weak problem statements.
   - Summarize investigation.
   - Draft CAPA.
   - Flag missing evidence.
   - Require human approval before writing decisions.

4. Add plant hierarchy:
   - Site, area, line, asset, station, shift.

5. Add role views:
   - Operator: report and contain.
   - Supervisor: assign and escalate.
   - Quality: validate root cause and CAPA.
   - Maintenance: work-order linked actions.
   - Leadership: portfolio and recurrence.

### P2 - Platform-Level Differentiators

1. Cross-module intelligence:
   - Pull OEE losses, Andon stops, maintenance events and Gantt constraints into RCA.

2. Recurrence intelligence:
   - Detect repeated causes by asset, line, material, supplier or shift.

3. Digital twin hooks:
   - Show related assets, process states and probable cause clusters.

4. Visual Obeya mode:
   - Large-screen team review board with RCA, actions, KPI trend and escalation.

## Design System Backlog

Create reusable UI primitives before building many screens:

- `CommandHeader`
- `MetricTile`
- `StatusChip`
- `SeverityPill`
- `IndustrialTimeline`
- `EvidenceCard`
- `CauseCard`
- `ActionCard`
- `FishboneBoard`
- `AIAssistantPanel`
- `EmptyState`
- `OfflineBanner`
- `AuditTrail`

These should later become shared components across modules.

## Success Criteria

The UI/content improvement is successful when a user can answer these questions in under 30 seconds:

- What happened?
- Where did it happen?
- How bad is it?
- Is production contained?
- What are the leading suspected causes?
- What evidence supports or rejects each cause?
- Who owns the next action?
- Is the action overdue?
- What must happen before closure?
- Has this happened before?

## Immediate Recommendation

Next implementation step: build the incident detail screen as a true command center.

This gives the biggest visible jump while also preparing the domain for AI, Gantt, OEE, Andon, TPM and maintenance integrations.
