# Connecting Yumney to Claude

Yumney exposes its toolset as an [MCP](https://modelcontextprotocol.io/) server at `https://yumney-gateway.<env>.azurecontainerapps.io/mcp`. Two paths get you connected; pick the one that matches your Claude product.

## Path A — Claude.ai (web, Pro, Team)

Claude.ai speaks MCP over HTTP/SSE natively. The OAuth handshake is built in — Yumney just needs to advertise the right metadata, which it does.

1. Open Claude.ai → **Settings** → **Connectors** → **Add custom connector**.
2. **Name:** `Yumney` (or whatever you'd like to see in the tool picker).
3. **URL:** `https://yumney-gateway.<env>.azurecontainerapps.io/mcp`
   - Staging: `https://yumney-gateway.calmsky-ae1ea5be.canadacentral.azurecontainerapps.io/mcp`
   - Production: TBD (Phase 4 rollout)
4. Click **Connect**. Claude.ai will:
   - GET `/.well-known/oauth-protected-resource` to discover the Keycloak realm
   - Register itself via RFC 7591 Dynamic Client Registration (using the IAT you supply when prompted — see "Getting an initial access token" below)
   - Open the Keycloak login screen in a popup
   - After you sign in + consent, the connector shows **Connected** and the tool list is live.
5. In a new chat, ask: _"What's on my Yumney shopping list?"_ — Claude should pick `get_merged_shopping_list`, confirm the call, and return the parsed list.

### Getting an initial access token

DCR on the `yumney` realm requires an IAT issued by the Yumney admin. Ping the admin (or run the script yourself if you have admin credentials):

```bash
export KEYCLOAK_PASSWORD='…'
./scripts/keycloak-mint-initial-access-token.sh --count 1 --expires 3600
```

The script prints the token on stdout. Paste it into Claude.ai's "Initial access token" prompt during connector setup. The token is consumed on first use; subsequent re-connects use the `registration_access_token` Claude stores automatically.

## Path B — Claude Desktop (Mac / Windows)

Claude Desktop is stdio-only today, so it needs the [`mcp-remote`](https://github.com/geelen/mcp-remote) bridge to talk to a remote HTTP MCP server.

### 1. Install mcp-remote

```bash
npx -y mcp-remote@latest --help   # one-time sanity check, no install needed
```

### 2. Edit `claude_desktop_config.json`

| Platform | Path |
|---|---|
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |

Add the `yumney` server entry:

```json
{
  "mcpServers": {
    "yumney": {
      "command": "npx",
      "args": [
        "-y",
        "mcp-remote@latest",
        "https://yumney-gateway.<env>.azurecontainerapps.io/mcp"
      ]
    }
  }
}
```

### 3. Restart Claude Desktop

On first connect, `mcp-remote` opens your default browser to the Keycloak login page. Sign in, click **Allow** on the consent screen, and the bridge stores the OAuth tokens locally. Subsequent launches reuse them.

If your config already has other `mcpServers`, merge the `yumney` entry into the existing block — don't replace the whole `mcpServers` object.

## Sample prompts

These exercise interesting tools end-to-end:

- _"What recipes can I cook with what's on hand?"_ → `get_cookable_recipes`
- _"Plan my week around Italian recipes I haven't cooked in 30 days"_ → `suggest_week_plan` (filtered) → `assign_meal` per day
- _"How many recipes did I cook last month?"_ → `get_meal_analytics`
- _"Save this recipe: https://en.wikipedia.org/wiki/Carbonara"_ → `import_recipe_from_url`
- _"Make a shopping list for spaghetti carbonara and beef ragù"_ → `create_shopping_list_from_recipes`
- _"What's left on my shopping list?"_ → `get_merged_shopping_list`

The full tool list is at `https://yumney-gateway.<env>.azurecontainerapps.io/discovered-capabilities` (anonymous endpoint, JSON dump).

## Troubleshooting

### `401 Unauthorized` on every tool call

The OAuth token expired (Yumney access tokens live 1 h by default). Claude.ai will refresh automatically on the next call; for Claude Desktop, restart the app to trigger `mcp-remote`'s refresh flow. If it persists, check the `WWW-Authenticate` header on the 401 — it should point at `/.well-known/oauth-protected-resource`. If it doesn't, the connector is talking to a stale URL.

### `403 Forbidden`

Authentication succeeded but you don't have the required scope. Yumney tools all require the `yumney-api` audience; if the IAT issued to your connector didn't include it, re-mint with `scope=openid profile email yumney-api`.

### Tool calls hang past 60 s

YARP's `ActivityTimeout` on the `/mcp` route is 10 min, so this isn't a proxy timeout — the upstream module call is genuinely slow. Check the App Insights trace (filter `mcp.tool.duration > 10000`) for which tool is bottlenecked.

### Rate-limit `429`

The MCP server caps at 60 tool calls per minute per user. The 429 response body has a `retry_after_seconds` field; Claude reads it and backs off automatically. If you're hitting the limit legitimately, file an issue — the ceiling is revisable.

### Where to find logs

- **Staging Aspire dashboard:** `https://yumney-aspire.<env>.azurecontainerapps.io` — full structured logs across `mcp-server`, `recipes-api`, etc.
- **App Insights:** filter by `cloud_RoleName == 'mcp-server'` to see all MCP-related telemetry; relevant metrics: `mcp.tool.invocations`, `mcp.tool.duration`, `mcp.rate_limit.rejections`.
- **Trace timeline:** `mcp.tool/{name}` spans link to their upstream HTTP call via W3C traceparent — open one in App Insights and the upstream module's request is the child span.

## What's not in this guide

- **ChatGPT custom GPTs** — same OAuth/DCR flow, different setup UI; doc to follow once Phase 4 (per-platform submission) lands. The technical path is identical.
- **VS Code MCP** — Yumney's tools work from the VS Code MCP client too, but the marketing-facing setup steps live elsewhere.
