# MCP Tool Versioning Policy

External MCP clients (Claude.ai connectors, Claude Desktop, ChatGPT custom GPTs, Cursor, …) cache the tool list on first connect and call by name forever after. Changing a tool's name, removing a required field, or shifting its semantics is a public breaking change for every installed client. This doc is the contract for evolving the surface without breaking users.

## When to bump the version

| Change | Bump version? |
|---|---|
| Add an optional request field | No |
| Add a new response field | No |
| Loosen a validation rule (e.g. raise max length) | No |
| Add a new tool | No (it's a new tool with its own v1) |
| Rename a tool | **Yes** |
| Remove a required request field, or make one required that wasn't | **Yes** |
| Change a field's type / shape | **Yes** |
| Change semantics ("returns top 10" → "returns top 20") | **Yes** if downstream behaviour relies on it |
| Tighten a validation rule | **Yes** |

When in doubt, bump — the cost of a stale-name suffix is much smaller than a silent breakage of installed connectors.

## How to bump

1. Keep the original endpoint + capability registration in place. Do **not** edit the existing `WithCapability(name: …)` call.
2. Add a new endpoint with the new shape. Register it with the same `name:` but `version: 2` (or higher):
   ```csharp
   group.MapPost("/v2/recipes", SaveRecipeV2)
       .WithName("SaveRecipeV2")
       .WithTags("Recipes")
       .WithCapability(
           name: "save_recipe",
           description: "Import a recipe with the new ingredient-quantity grammar — replaces save_recipe v1. …",
           surfaces: CapabilitySurface.Chat | CapabilitySurface.Mcp,
           version: 2);
   ```
3. The MCP server projects this onto the wire as `save_recipe__v2`. v1 continues to be advertised as `save_recipe`. Both tools appear in `tools/list`; old clients keep calling `save_recipe`, new clients pick `save_recipe__v2`.
4. Update the v1 endpoint's `description` to call out the deprecation:
   ```csharp
   description: "DEPRECATED — superseded by save_recipe__v2 on 2026-08-01. … (original text)"
   ```
   The deprecation note shows up in `tools/list` and tells the LLM to prefer the new version when both are visible.

## Removal policy

A deprecated tool version stays available for **at least 90 days** after its replacement ships. The clock starts when the replacement endpoint is merged to `develop`. Document the deprecation date in the source comment above the old `WithCapability` call so any future contributor can see when removal becomes safe:

```csharp
// Deprecated 2026-08-01 in favour of v2 — earliest removal 2026-10-30.
group.MapPost("/recipes", SaveRecipeV1)
    .WithCapability(name: "save_recipe", …);
```

Removing before the 90-day window means breaking any client config that hasn't been touched in three months. Don't.

## What the wire looks like

| Version | Tool name on wire | Notes |
|---|---|---|
| 1 (default) | `{name}` | Unchanged — installed configs keep working |
| 2 | `{name}__v2` | New connections discover via `tools/list` |
| ≥ 3 | `{name}__v{Version}` | Same pattern as v2 |

The `__v{n}` suffix is parsed by `CapabilityToolRegistration.WireName(...)` and is the only place the version leaks into the protocol shape. Routes, OpenAPI paths, and module endpoint URLs are independent — pick whatever URL scheme the module module owner prefers (e.g. `/api/v2/recipes`).

## What is NOT versioned

- **Surface flags** (`CapabilitySurface.Chat | CapabilitySurface.Mcp`). Changing whether a tool shows up on MCP isn't a name-versioning concern — it's either visible or not.
- **Tool annotations** (`ReadOnlyHint`, `DestructiveHint`, …). These are hints; flipping them is not a breaking change.
- **Internal route changes** that don't affect the wire name or shape. Refactor freely.

## Architecture-test guard (planned)

A future architecture test will assert that no tool version has been removed within 90 days of its replacement going live. The mechanism: the deprecation date lives in a code comment above the old `WithCapability` call; the test grep-parses these comments and compares against `DateTime.UtcNow`. This isn't implemented today because there are no deprecated versions yet — wire it in when the first one ships.
