# Architecture Decision Records

Short, dated records of non-trivial architectural choices. Each ADR
captures the context, the decision, and the consequences in enough
detail that a future contributor can tell whether the decision still
applies.

## Index

- [0001 — Persistence paradigm per aggregate](0001-persistence-paradigm-per-aggregate.md)
- [0002 — Capability surface for chat, MCP, and other LLM clients](0002-capability-surface-for-chat-mcp.md)
- [0003 — LLM access via Microsoft.Extensions.AI](0003-llm-access-via-microsoft-extensions-ai.md)

## Conventions

- Filename: `NNNN-kebab-case-title.md`, zero-padded sequential number.
- Status: `Proposed` → `Accepted` → `Superseded by NNNN` when replaced.
- Keep it short. If an ADR needs more than two screens, it's probably
  two decisions.
- Reference the originating issue or PR in the Context section.
