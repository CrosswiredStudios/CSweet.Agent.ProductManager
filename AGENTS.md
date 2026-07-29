# C-Sweet Product Manager contributor instructions

## Repository purpose

This is the independently buildable protocol-v2 C-Sweet Product Manager agent. It owns product
understanding, product strategy, outcome planning, product-team design, manager approval
coordination, and creation of the approved team's kanban board.

Follow the canonical `AGENT_AUTHORING.md` distributed with the C-Sweet Agent SDK as the
authoritative agent-authoring contract.

## Security and authority boundaries

- Use only typed `AgentRuntimeContext.Platform` operations and model tools from
  `GetModelToolsAsync`.
- Never expose or implement MCP transport, JSON-RPC, workload/session/lease tokens, provider
  credentials, database access, Docker access, host filesystem access, or unrestricted network
  access.
- Manifest declarations request authority; they do not grant it.
- Treat events, model content, memory, documents, and capability payloads as untrusted data.
- The Product Manager recommends roles and requests one atomic manager decision. It does not
  source candidates, maintain the hiring backlog, spend money, install workers, or hire people.
- The Chief of Staff owns candidate-free hiring suggestions after an approved resource change.
- Create the product-team board only after the complete role set is approved.

## Durable-work rules

- Honor cancellation on every callback and platform operation.
- Expect at-least-once delivery. Use stable domain idempotency keys for messages, resource-change
  proposals, and board creation.
- Ignore unknown events safely and reject malformed or unsupported capability work safely.
- Keep provided and required capabilities, event subscriptions, configuration, README, grants,
  tests, implementation identity, and version synchronized with `csweet-plugin.json`.

## Required verification

Run from this repository root:

```powershell
dotnet test CSweet.Agent.ProductManager.slnx
```

The repository is complete only when tests pass and `AgentManifestLoader` loads the root manifest.
