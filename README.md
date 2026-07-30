# C-Sweet Product Manager

First-party Product Manager agent for C-Sweet, built on `CSweet.Agent.SDK` 2.2.0 and manifest protocol v2.
The agent package version is `1.3.1`.

It owns product outcomes, customer discovery, product strategy, prioritization, roadmaps, requirements, success measures, delivery alignment, and product-team design. It does not choose technical architecture, write production code, make legal conclusions, source candidates, hire workers, or spend money.

## Runtime behavior

The agent receives exact-installation durable events and capability work. Its primary startup goal is
to understand the authoritative product context and then recommend the smallest appropriate
product team. Onboarding validates its employee/reporting identity, opens or reuses the manager
conversation, and uses its configured model to compose a business-specific opening from authoritative
operating context and relevant approved organization and relationship memory. It identifies the
deliverable it believes it owns, asks only one genuinely missing clarification when necessary, then
obtains and reviews the Chief of Staff role brief when that agent is its manager and submits one
atomic team snapshot for an explicit manager decision when the plan is ready. A deterministic,
contextual message is used only when model generation is unavailable.

Requested revisions are applied and resubmitted when an authoritative constraint makes the change
deterministic; otherwise the Product Manager asks its manager one focused refinement question.
After approval it creates one idempotent, appropriately named product-team kanban board. The Chief
of Staff then owns creation of one candidate-free hiring suggestion per approved added or increased
role, making the same approved role set visible in the Hiring tab and in the Chief's manager
conversation.

Chat chunks are durable progress. Configuration and final responses are durable results. Typed
platform calls and model tools always reflect the current grant revision.

Chief of Staff coordination uses install-time, same-organization capability bindings. Payload identities remain untrusted and neither agent can select a target installation. Provider credentials and runtime transport details never enter agent code.

When an executive finalizes a product-team recommendation outside the Product Manager's reporting
conversation, the runtime resolves the current manager and routes the atomic resource-change
request into the protected manager chat. Agent managers receive a targeted durable event; human
managers retain the stricter requirement that approval originate from their direct conversation.

## Build and test

```powershell
dotnet build CSweet.Agent.ProductManager.slnx
dotnet test CSweet.Agent.ProductManager.slnx
```

Requirements are .NET 10, `CSweet.Agent.SDK` 2.2.0, `CSweet.Memory`, an approved protocol-v2 installation, an active managing employee, and the grants in [GRANTS.md](GRANTS.md).

## SDK 1.1.1 authoring contract

The protocol-v1 transport APIs were removed. The implementation now uses `AgentEventEnvelope`, `AgentCapabilityRequest`, `AgentWorkResult`, typed `AgentRuntimeContext.Platform` calls, `ReportProgressAsync`, live model tools, and `PlatformChatClient`. The v2 manifest adds schemas, timeouts, and idempotency and removes generic publications.

## Provided capability behavior

Assistant, planning, and check-in callbacks may emit progress and always produce a durable result.
`product-management.plan.v1` is advisory and idempotent per work item.
`product-management.context.update.v1` records authoritative context and, when ready, submits the
refreshed complete team through the separately granted resource-change capability. Configuration
update changes runtime configuration. External communication, approvals, board creation, and other
effects occur only through separately granted platform capabilities. See [GRANTS.md](GRANTS.md).
