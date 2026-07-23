# C-Sweet Product Manager

See [`GRANTS.md`](GRANTS.md) for the complete capability catalog organized by service and feature.

This is the first-party C-Sweet Product Manager agent. It uses the same GitHub import, isolated
build, broker authorization, employee binding, configuration, memory, and management-cycle path as
other C-Sweet agents; it is not a privileged runtime.

The Product Manager owns product outcomes, customer discovery, product strategy, prioritization,
roadmaps, requirements, success measures, delivery alignment, and the product-team design. It does
not choose technical architecture, write production code, make legal conclusions, run campaigns,
source candidates, or hire workers.

## Reporting model

The Product Manager must report to an active managing employee. That manager may be the CEO, the
Chief of Staff, another human employee, or another agent.

On its durable onboarding event it:

1. validates its employee and reporting relationship against the organization snapshot;
2. reuses the protected hiring conversation when the hiring employee is its manager, or opens a
   direct conversation with its manager;
3. sends an idempotent request for its mandate, product and customer context, desired outcomes,
   success measures, decision rights, team context, and constraints;
4. when the manager is an active Chief of Staff agent, also requests the structured role brief and
   routes gaps or the initial product/team recommendation through the Chief coordination contracts;
   and
5. acknowledges onboarding only after the manager message has been persisted.

The Product Manager then waits for manager direction and remains event-driven. When a Chief of
Staff is the manager, the Chief remains responsible for CEO escalation, company-wide organization
design, the ranked hiring backlog, candidate sourcing, hiring workflows, spending, and approvals.
The Chief can later consult the Product Manager through `product-management.plan.v1` and push
idempotent role updates through `product-management.context.update.v1`.

## Requirements

- .NET 10 SDK
- `CSweet.Agent.SDK` 0.6.0
- `CSweet.Memory` 0.1.1
- A C-Sweet broker and an approved installation
- An active managing employee and the communication capabilities declared in the manifest
- For structured Chief coordination, an active Chief of Staff manager with the management
  capabilities declared in the manifest

## Build and test

```powershell
dotnet build CSweet.Agent.ProductManager.slnx
dotnet test CSweet.Agent.ProductManager.slnx
```

For local SDK and memory development, place this repository next to `CSweetAgentSdk` and
`CSweet.Memory`; `Directory.Build.props` automatically uses those project references when present.

## Import

Push this repository and import its GitHub URL through C-Sweet. Review the manifest grants before
approval. Provider credentials remain inside C-Sweet and are never supplied to the agent process.

At runtime the broker supplies the employee display name, role, manager, installation identity, and
organization boundary. Direct manager messages target the current reporting relationship.
Agent-to-agent coordination requests target exact installation IDs and validate the current
reporting line instead of trusting payload identity.
