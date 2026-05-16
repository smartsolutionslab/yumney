# Connecting Yumney to ChatGPT

ChatGPT offers two integration paths today:

| Path | Spec | Status |
|---|---|---|
| GPT Action (OpenAPI) | OpenAPI 3.1 spec + OAuth2 | Generally available |
| Native MCP | Same `.well-known` discovery as Claude | Rolling out |

Path A (GPT Action) reaches the much larger ChatGPT user base today; Path B piggybacks on Phase 1 work and is essentially free once it ships broadly.

## Path A — Custom GPT with an OpenAPI Action

1. Go to https://chatgpt.com/gpts/editor and click **Create**.
2. On the **Configure** tab fill in:
   - **Name:** Yumney
   - **Description:** Recipe import, meal planning, shopping list management.
   - **Instructions:** _"You help the user manage their recipe collection, plan meals for the week, and assemble shopping lists. Always confirm before deleting recipes or finalising a meal plan."_
   - **Conversation starters:** see the sample prompts in [claude-setup.md](claude-setup.md).
3. Scroll to **Actions** → **Create new action**.
4. **Authentication:** OAuth.
   - **Client ID:** `yumney-chatgpt` (or `yumney-web` if you don't want to provision a dedicated client)
   - **Client Secret:** the one from Keycloak → Clients → yumney-chatgpt → Credentials
   - **Authorization URL:** `https://yumney-keycloak.<env>.azurecontainerapps.io/realms/yumney/protocol/openid-connect/auth`
   - **Token URL:** `https://yumney-keycloak.<env>.azurecontainerapps.io/realms/yumney/protocol/openid-connect/token`
   - **Scope:** `openid profile yumney-api`
   - **Token Exchange Method:** Default (POST request)
5. **Schema:** click **Import from URL** and paste:
   `https://yumney-gateway.<env>.azurecontainerapps.io/openapi/v1.json`
   This URL is served by `Yumney.Mcp.Server` and returns an OpenAPI 3.1 spec covering exactly the MCP-surface capabilities (filtered from the per-module manifests; same set `RestProxyService` invokes).
6. The OpenAI builder displays the parsed operations and a callback URL like `https://chat.openai.com/aip/<action-id>/oauth/callback`. Copy that callback URL and add it to the Keycloak client's **Valid redirect URIs** list.
7. Save the GPT. On first call, ChatGPT triggers the OAuth dance; sign in with your Yumney credentials, click **Allow** on the consent screen, and the action is live.

### Sample first call

In a new chat with the GPT, ask: _"What's on my Yumney shopping list?"_ — the GPT calls `get_merged_shopping_list` and returns the parsed list.

## Path B — Native MCP (when it lands more broadly)

Same flow as [Claude.ai](claude-setup.md#path-a--claudeai-webproteam): paste the `/mcp` URL, let ChatGPT discover via `/.well-known/oauth-protected-resource`, complete the OAuth handshake. We don't have a UI walkthrough for this path yet because OpenAI's native-MCP UI is still in flux.

## OpenAPI schema limits

The spec served at `/openapi/v1.json` is a minimal-but-valid OpenAPI 3.1 document:

- Operations cover every MCP-surface capability discovered by the MCP server.
- Path parameters are extracted from the route templates (`{identifier:guid}` → `{identifier}`, `{year:int}` → `{year}`).
- Request bodies on POST/PUT/PATCH are `application/json` with `additionalProperties: true` — ChatGPT will pass through whatever fields the LLM thinks the tool expects. The tool description text is the authoritative source for field shapes.
- OAuth2 security points at the configured Keycloak realm.

What's missing (planned follow-up): per-operation request/response **schemas**. Today they're not aggregated from the per-module OpenAPI specs; the model relies on description text. For tools with non-obvious payloads (`save_recipe`, `create_shopping_list_from_recipes`), expect the model to occasionally guess the field names. When that happens, paste the curl example into the GPT's instructions as a hint.

## Submitting to the GPT Store

Once the GPT works end-to-end against staging, repeat the setup against production and submit it via **Share** → **Publish to the GPT Store**. OpenAI reviews can take a few days; in the meantime use the private share link to onboard early users.

## Troubleshooting

Same as the Claude doc — token expiry symptoms, 401/403 causes, 429 rate-limit body, where to find logs. See [claude-setup.md → Troubleshooting](claude-setup.md#troubleshooting).
