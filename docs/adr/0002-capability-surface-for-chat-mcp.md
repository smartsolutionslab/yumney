# ADR 0002 — Capability surface for chat, MCP, and other LLM clients

- Status: Proposed
- Date: 2026-05-10
- Context: Architecture review of US-184 to US-190 (chat command routing); confirmed near-term need for external LLM-client integration (Claude Desktop / 3rd-party assistants) and additional in-app surfaces beyond the chat panel (voice, future CLI).

## Context

User stories US-184 through US-190 require the in-app chat panel to **execute capabilities**, not just generate text:

- US-184 "What can I cook?" — read the ingredient balance, match against the user's recipes, return ranked suggestions.
- US-185 Planner Mode — chat-driven meal planning that mutates `WeeklyPlan`.
- US-186 Cook Now — chat starts the cook-mode flow for a chosen recipe.
- US-187 / US-188 / US-189 — additional chat commands (history, ratings, list maintenance).
- US-190 Recipe Search & Navigation — chat invokes recipe search and navigates the SPA.

The `SemanticKernelChatService` currently produces a free-text reply and post-extracts recipe titles by regex. `ParseIntentCommandHandler` exists at `/api/v1/recipes/parse-intent` but is unused by the chat endpoint, so no intent is dispatched to a handler. A separate finding is that nearly all the underlying handlers needed by US-184 to US-190 (`GetCookableRecipesQueryHandler`, `GetIngredientBalanceQueryHandler`, `GetRecipesQueryHandler`, `AssignRecipeCommandHandler`, `ConfirmMealCommandHandler`, etc.) **already exist** — the gap is purely the chat-to-handler dispatch path.

In parallel, two scope expansions were confirmed in the design discussion:

1. **External LLM clients** (Claude Desktop, ChatGPT custom GPTs, Slack assistants) should be able to call Yumney capabilities on behalf of an authenticated user.
2. **Additional in-app surfaces** (voice command, future CLI) should reuse the same capability set instead of reimplementing the dispatch.

Three architectural options were considered:

| Option | Shape | Fit |
|---|---|---|
| A — Custom intent router | LLM emits `{intent, entities}`; backend `switch` dispatches; response carries text + structured payload. | Works for in-app chat only. Every external surface re-implements dispatch. |
| B — Semantic Kernel function calling only | LLM picks `KernelFunction`s automatically. Works in-process. | Solves in-app chat cleanly. No external integration story; tied to SK. |
| C — MCP server only | Expose capabilities via Model Context Protocol; clients (incl. in-app chat) call via MCP. | Solves external integration. Heavyweight for the in-app chat path that already runs SK in-process. |

## Decision

**Define the capability set once as a curated subset of the existing per-module REST endpoints; expose it through two adapters: Semantic Kernel function calling for in-app chat, and an MCP server for external clients. Client-only UI directives ride alongside as a separate response channel.**

Concretely:

### 1. The capability surface IS the existing OpenAPI surface (curated)

- Each module's `*.Api` host already publishes an OpenAPI document via Scalar.
- Selected endpoints are tagged with a `[Capability(name, description)]` attribute that flags them as chat / MCP / voice-callable and provides a stable LLM-facing name (e.g. `search_recipes`, `get_cookable_recipes`).
- The capability manifest is generated from the OpenAPI surface at build time. Endpoints without the attribute are not exposed to LLM clients.
- This keeps a single source of truth: the REST contract is the tool contract.

### 2. Server-executable capabilities vs. client-only UI directives

Two distinct response channels:

- **Server-executable capabilities** (data and side effects) — live as REST endpoints, work for every surface (in-app chat, MCP, voice, CLI). Examples: `search_recipes`, `get_cookable_recipes`, `assign_meal`, `confirm_meal_cooked`, `get_ingredient_balance`.
- **Client-only UI directives** (navigation, mode switches, scroll-to) — meaningless to an external MCP client because they require the Yumney Angular shell. Carried as `ChatResponseDto.Actions[]` with shapes like `navigate(route)`, `start_cook_mode(recipeId)`, `open_recipe(recipeId)`. Interpreted by a small action dispatcher in the shell. Never exposed via MCP.

### 3. Adapters

- **In-app chat: Semantic Kernel function calling.** `SemanticKernelChatService` registers a `KernelFunction` per capability, wrapping either an in-process command/query handler (when collocated, as Recipes is with Chat) or a sibling-module REST call via the existing consumer-defined-contracts pattern. SK's auto function-calling picks tools from the LLM response. The chat reply still carries free text, plus optional `Actions[]` for UI directives.
- **External clients: `Yumney.Mcp.Server` (new ASP.NET Core host).** Bridges to Keycloak via OAuth (the MCP spec's standard auth flow), aggregates per-module capability manifests, and proxies tool calls to the corresponding REST endpoints with the user's bearer token. Treated as a peer consumer of each module — no project references into module Application/Infrastructure layers, same rule as the Angular shell.
- **Future surfaces** (voice, CLI) consume the same capability surface either via MCP (when remote) or via SK function calling (when collocated).

### 4. Where code lives

- `[Capability(...)]` attribute and manifest generation: new tiny project `Yumney.Shared.Capabilities` (Application-layer abstractions, no module deps).
- SK function registrations: `Yumney.Recipes.Extraction` (where chat already lives). Functions targeting handlers in other modules call those modules over HTTP via the consumer-defined-contracts pattern.
- `ChatResponseDto.Actions[]`: extended in `Yumney.Recipes.Application/DTOs/`.
- Frontend action interpreter: `client/libs/shared/chat/` (new sub-feature).
- MCP server: `src/Yumney.Mcp.Server/` (new host, registered in `Yumney.AppHost` and routed by `Yumney.Gateway`).

## Consequences

**Positive**

- Single source of truth for the capability surface (the REST contract). LLM tool descriptions can't drift from the actual endpoint shape.
- External integration (Claude Desktop, etc.) and in-app chat share the same surface — adding a capability lights it up in both places.
- Future surfaces (voice, CLI) get the dispatch for free.
- Client-only UI directives stay separate from the externally-visible surface, preventing accidental promises that "navigate to shopping list" works from Claude Desktop.
- Does not violate cross-module rules: the MCP server is a peer HTTP consumer, not a sibling module.

**Negative**

- Two adapter codepaths to maintain (SK function calling + MCP server). The shared OpenAPI manifest mitigates drift but does not eliminate per-adapter wiring (parameter shape, auth headers, error mapping).
- A capability tagged for LLM use is now part of an external contract — renaming or removing one is a breaking change for installed MCP clients.
- The MCP server adds a new public network surface and a new auth-flow path (OAuth installation for Claude Desktop and similar).
- SK function calling currently requires an LLM provider that supports tools natively (OpenAI, Anthropic via Semantic Kernel connector, Azure OpenAI). Provider portability narrows.

## Implementation order

Tracked under the corresponding GitHub epic. Roughly:

1. **`ChatResponseDto.Actions[]` + frontend action interpreter** — unblocks navigation half of US-190 and `start_cook_mode` for US-186 even without any LLM-driven dispatch (deterministic intent → action mapping using the existing `ParseIntentCommandHandler`).
2. **SK function calling for in-app chat** — wraps the ~12 existing handlers as `KernelFunction`s, switches `SemanticKernelChatService` from regex post-match to LLM-driven dispatch. Unblocks US-184, US-185, US-187 to US-190 server-side.
3. **`[Capability]` attribute + manifest generation** — extracts the tagging into its own primitive; manifest lands in OpenAPI as a vendor extension.
4. **`Yumney.Mcp.Server` standup** — new host; OAuth bridge; tool descriptors from manifest; proxy to REST endpoints. Separate epic from in-app chat work.

## Follow-ups

- Decide on per-tool Keycloak scopes vs. one umbrella `yumney.api` scope before MCP standup. Default: one scope, escalate when an integration partner requires least-privilege.
- Document the capability versioning policy (rename = new tool name + retain old as alias for one minor) once the first external client is in production.
- Revisit this ADR when SK is replaced with a different orchestration layer, or when the MCP spec ships a breaking version.
