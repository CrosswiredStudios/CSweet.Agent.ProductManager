# C-Sweet Product Manager

First-party Product Manager agent for C-Sweet, built on `CSweet.Agent.SDK` 1.0 and manifest protocol v2.

It owns product outcomes, customer discovery, product strategy, prioritization, roadmaps, requirements, success measures, delivery alignment, and product-team design. It does not choose technical architecture, write production code, make legal conclusions, source candidates, hire workers, or spend money.

## Runtime behavior

The agent receives exact-installation durable events and capability work. Onboarding validates its employee/reporting identity, opens or reuses the manager conversation, sends an idempotent direction request, optionally obtains an approved Chief of Staff role brief, and acknowledges only after persistence. Chat chunks are durable progress. Configuration and final responses are durable results. Typed platform calls and model tools always reflect the current grant revision.

Chief of Staff coordination uses install-time, same-organization capability bindings. Payload identities remain untrusted and neither agent can select a target installation. Provider credentials and runtime transport details never enter agent code.

## Build and test

```powershell
dotnet build CSweet.Agent.ProductManager.slnx
dotnet test CSweet.Agent.ProductManager.slnx
```

Requirements are .NET 10, `CSweet.Agent.SDK` 1.0, `CSweet.Memory`, an approved protocol-v2 installation, an active managing employee, and the grants in [GRANTS.md](GRANTS.md).

## SDK 1.0 migration

The protocol-v1 transport APIs were removed. The implementation now uses `AgentEventEnvelope`, `AgentCapabilityRequest`, `AgentWorkResult`, typed `AgentRuntimeContext.Platform` calls, `ReportProgressAsync`, live model tools, and `PlatformChatClient`. The v2 manifest adds schemas, timeouts, and idempotency and removes generic publications.

## Provided capability behavior

Assistant, planning, and check-in callbacks may emit progress and always produce a durable result. `product-management.plan.v1` is advisory and idempotent per work item. `product-management.context.update.v1` records authoritative context and indicates whether a plan refresh is needed. Configuration update changes runtime configuration. External communication, approvals, and other effects occur only through separately granted platform capabilities. See [GRANTS.md](GRANTS.md).
