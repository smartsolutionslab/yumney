# Submitting Yumney to the MCP Registry

The Model Context Protocol community maintains a public registry of MCP servers — listed servers are surfaced inside MCP-aware clients (Claude.ai connector picker, MCP browser tools) and on the registry website. This runbook walks through submitting Yumney and re-submitting when something material changes (public URL, capability surface, brand assets).

## When to (re-)submit

| Trigger | Action |
|---|---|
| First Phase 4 production launch | Initial submission. |
| Public URL changes (new ACA environment, custom domain) | Update the existing registry entry — clients cache the URL. |
| Major capability addition (new module exposed to MCP) | Optional bump of the description/tags; not strictly required. |
| Logo / icon refresh | Update the asset URL in the registry entry. |
| Outage or known breakage | Temporarily delist if it'll be down >24 h. Clients show a broken-tool indicator otherwise. |

## Pre-flight checklist

Before submitting, verify the live server passes the registry's smoke tests:

- [ ] `GET /.well-known/oauth-protected-resource` returns the RFC 9728 document with the canonical resource URL
- [ ] `POST /mcp` with no Authorization header returns **401** carrying a `WWW-Authenticate` header that references the discovery URL
- [ ] OAuth flow completes against Keycloak: register a throwaway DCR client (`./scripts/keycloak-mint-initial-access-token.sh`), complete the authorization-code dance, exchange for an access token
- [ ] With a valid bearer, `tools/list` returns the curated MCP surface (sanity-check the names match what `Yumney.Mcp.Server.Tests.Mcp.CapabilityToolRegistrationTests` asserts)
- [ ] At least one `tools/call` round-trips successfully (e.g. `get_merged_shopping_list` with a test user that owns a list)
- [ ] `mcp.tool.invocations` counter appears in App Insights after that call

If any of these fail, fix before submitting — the registry rejects servers that don't behave per spec.

## Submission

1. Open the registry's submission form (URL depends on which registry — `https://registry.modelcontextprotocol.io` if the community settled there; otherwise the URL the Anthropic / OpenAI announcements pointed at).
2. Fill in:
   - **Name:** Yumney
   - **Description:** _"From URL to cook-ready recipe in seconds — no ads, no blabla. Import recipes from any URL, plan meals for the week, assemble shopping lists, all from Claude or ChatGPT."_ (mirrors the README marketing copy)
   - **Public URL:** `https://yumney-gateway.calmsky-ae1ea5be.canadacentral.azurecontainerapps.io/mcp` (staging) or the production URL once Phase 4 ships
   - **Logo:** the brand SVG/PNG; host it on the marketing site or repo raw URL, not on a CDN that requires auth
   - **Tags:** `recipes`, `meal-planning`, `shopping`, `food`
   - **Connect / setup link:** `https://github.com/smartsolutionslab/yumney/blob/main/docs/mcp/claude-setup.md` (Phase 4.1 doc; once we have a marketing site, point at that instead)
   - **Auth type:** OAuth2 (Keycloak)
   - **Maintainer email:** the on-call address
3. If the registry asks for an attestation that the server is RFC-9728-compliant, link the pre-flight checklist above.
4. Submit. Most registries auto-test the discovery doc + OAuth metadata and approve in minutes; some are manually reviewed and take days.

## After submission

- Add the registry listing URL to the README so users can find it both ways.
- Add a Grafana / App Insights alert on `mcp.tool.invocations` dropping to zero for >24 h — registry exposure means random users will exercise the server, and a flat-line is a strong outage signal.
- When the production deploy happens, update the entry's URL field (don't create a new listing — that orphans existing client installs).

## What we do NOT submit

- Internal-only capabilities (dashboard reset, MigrationRunner) — these are filtered out by `CapabilityRouteBuilderExtensions.WithCapability(... surfaces: ...)` at registration time, so the published `tools/list` only contains user-facing tools. If something internal leaks, fix the surface flag before re-submitting.
- The staging URL alongside production — registries usually take one URL per entry; staging stays internal.
