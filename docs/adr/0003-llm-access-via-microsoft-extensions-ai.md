# ADR 0003 — LLM access via Microsoft.Extensions.AI

- Status: Accepted
- Date: 2026-10-30
- Context: LLM/MCP refactoring US-000 (PRs #833 – #837); amends the in-app chat adapter of [ADR 0002](0002-capability-surface-for-chat-mcp.md)

## Context

All LLM calls went through Microsoft Semantic Kernel (SK). In practice SK was used only as a chat-completion wrapper plus function calling — no planners, prompt templates, memory or agents — and that thin usage carried real costs:

- Provider wiring was copy-pasted into three `*.Extraction` projects, each with its own `SemanticKernelOptions`, and it drifted: shopping-api never received LLM configuration (every unknown item became "other"), and the Ollama path could not start (Aspire's `Endpoint=…;Model=…` connection string was passed to `new Uri(...)`).
- Every service parsed free-text JSON by hand (three `ExtractJson` copies, repair prompts) because no structured output was requested.
- No GenAI telemetry, and a chat service with 16 constructor parameters.

SK itself is built on Microsoft.Extensions.AI (MEAI), which was already a transitive dependency. Aspire ships MEAI client integrations for OpenAI and Ollama.

## Decision

**All LLM access goes through MEAI's `IChatClient`; Semantic Kernel is removed.**

- **One registration point**: `Yumney.Shared.AI.AddYumneyChatClient()`. The AppHost gives each LLM host a model resource named `chat` (`AddOpenAI().WithApiKey().AddModel()` or `AddOllama().AddModel()`) plus `Llm__Provider`; the host maps it onto `Aspire.OpenAI` or `CommunityToolkit.Aspire.OllamaSharp`, which also wire OpenTelemetry. A host without a model logs an error at startup and gets a stand-in client that fails per call, so services stay resolvable.
- **Structured output**: one-shot services request a JSON schema generated from the same serializer options that parse the reply (`LlmJson.OptionsFor<T>()` / `LlmJson.Deserialize<T>()`). Parsing stays tolerant of code fences, and the one-shot repair retry remains as a safety net.
- **Recipe extraction keeps its flat recipe-or-`{"error":"NO_RECIPE_FOUND"}` shape**, because the streaming import endpoint and the frontend read top-level fields while they stream. Streaming uses plain JSON mode instead of a schema.
- **In-app chat** (amends ADR 0002 §3): tools implement `IChatTool` and are exposed as `AIFunction`s; `FunctionInvokingChatClient` runs the tool loop, capped at 5 round trips per message. Tool wire names match the MCP capability names. The MCP server is unchanged in role; it now publishes a JSON input schema per capability.
- Microsoft.Extensions.AI stays inside `*.Extraction` and `Yumney.Shared.AI` — enforced by `ExternalDependencyIsolationTests`, which also keeps SK out.
- **Provider flexibility through one OpenAI-compatible path**: `OpenAi:Endpoint` points the OpenAI client at any compatible API (OpenAI, DeepSeek, Qwen, Kimi, Mistral, Claude's compatibility endpoint, OpenRouter, Azure AI Foundry); `OpenAi:StructuredOutput` (`JsonSchema` default, `JsonObject`, `None`) downgrades schema requests in the client pipeline for providers that reject them. No provider-specific SDKs: an official Anthropic SDK adapter was built and dropped (#842) in favour of switching providers by configuration only.

Microsoft Agent Framework (also built on `IChatClient`) was considered and deferred: it adds agents, threads and multi-agent workflows, not provider flexibility — that comes from `IChatClient`. The chat is a single-agent tool loop, which `FunctionInvokingChatClient` covers. Adopting it later is additive.

## Consequences

**Positive**

- Provider configuration lives in the AppHost and one helper; switching to another OpenAI-compatible provider is configuration only.
- Schema-constrained replies on OpenAI and Ollama; fewer parse failures and no hand-maintained JSON in prompts as the only contract.
- The Aspire client integrations enable OpenTelemetry for chat calls (latency, token counts) by default; confirming the spans in the Aspire dashboard is part of the first real-model run.
- Services and tests depend on a small interface; `ScriptedChatClient` / `ToolCallingChatClient` in `Yumney.TestBuilders` replace SK kernel fakes, and the chat tests exercise the real tool loop.
- Shopping categorization batches all unknown items into one call per list.

**Negative**

- `Aspire.OpenAI` has no GA release; it is pinned to the same 13.6.4 preview revision as the Keycloak packages.
- Prompts still describe the JSON shape in prose; field-level guidance (e.g. allowed `difficulty` values) is not yet expressed as schema descriptions.
- Per-task models are opt-in configuration (`OpenAi:Models:fast` / `OpenAi:Models:vision` in the AppHost); until set, every task runs on the default model. Ollama always serves one model.
- Compatible providers differ in features (JSON schema, vision, tool calling). The structured-output mode covers the most common gap; each new provider/model still needs one real-model run of the import, suggestion, chat and categorization flows.
- Providers' own APIs may process data outside the EU (e.g. Chinese providers). EU-hosted alternatives for the same open-weight models are self-hosting via Ollama or Azure AI Foundry in an EU region.
