# Keycloak Dynamic Client Registration (DCR)

MCP clients (Claude.ai HTTP transport, ChatGPT custom GPTs, Claude Desktop) self-register with our auth server via RFC 7591 Dynamic Client Registration. This runbook covers how the staging admin enables a new client and how to revoke one.

## Why anonymous DCR is off

Keycloak's `/realms/yumney/clients-registrations/openid-connect` endpoint is reachable from the public internet. With **anonymous** DCR enabled, anyone can register a client and start probing the auth server — credential-stuffing the registration endpoint, exhausting client quotas, planting a redirect URI under attacker control.

We block the anonymous path with an empty `trusted-hosts` policy (see `src/Yumney.AppHost/Realms/yumney-realm.json` under `components`). Registration only succeeds when the request carries a valid **initial access token** (IAT) in `Authorization: Bearer …`.

## Policy guarantees for registered clients

The authenticated DCR policies (same `components` block) enforce:

| Policy | Effect |
|---|---|
| `allowed-protocol-mappers` | Whitelist of safe OIDC/SAML mappers; arbitrary custom mappers are rejected |
| `allowed-client-scopes` | Default scopes only — registered clients can't ask for elevated scopes |
| `max-clients` | Caps total clients in the realm at 200 |
| `client-uris` | Redirect URIs must be `localhost` / `127.0.0.1` / `claude.ai` / `chatgpt.com` patterns |
| `consent-required` | Forces the consent screen on first authorization |
| `scope` | Restricts the client to scopes the requesting user already has |
| `full-scope-disabled` | Registered clients don't inherit the full realm scope |

If a client needs a redirect URI outside the allowed-base-urls set, add it deliberately to the realm config — don't broaden the policy.

## Minting an initial access token

```bash
# Required: admin password (use the Aspire-dashboard-revealed value in dev,
# Key Vault in staging/production)
export KEYCLOAK_PASSWORD='…'

# Defaults: count=1, expires=86400 (24h). For an event-bound flow shrink expires.
./scripts/keycloak-mint-initial-access-token.sh --count 1 --expires 3600
```

The script prints just the token string on stdout. Hand it to the client operator over a confidential channel (1Password share, signed email — never Slack public channel).

Override target via env vars:

```bash
KEYCLOAK_URL=https://yumney-keycloak.<env>.azurecontainerapps.io \
KEYCLOAK_REALM=yumney \
KEYCLOAK_ADMIN=admin \
KEYCLOAK_PASSWORD='…' \
  ./scripts/keycloak-mint-initial-access-token.sh
```

## Client uses the token

The client POSTs to:

```
POST /realms/yumney/clients-registrations/openid-connect
Authorization: Bearer <IAT>
Content-Type: application/json

{
  "redirect_uris": ["http://localhost:9876/callback"],
  "grant_types": ["authorization_code"],
  "token_endpoint_auth_method": "none",
  "application_type": "native"
}
```

The response contains the generated `client_id`, `registration_access_token` (used for subsequent client updates), and the configured client URI. The IAT is consumed on first use; the client keeps the registration access token going forward.

## Revoking a misbehaving registered client

Log into the Keycloak admin console → realm **yumney** → **Clients** → find the registered client (the name is usually a UUID generated at registration) → **Delete**.

For audit, the registration timestamp + remote IP are in `event` logs (`Realm Settings → Events`). Filter by event type `CLIENT_REGISTER`.

If you suspect the IAT itself was leaked (handed out over a non-confidential channel), revoke unused IATs from **Realm Settings → Client registration → Initial access tokens**.

## Static clients are out of scope

`yumney-web` and `yumney-api` stay as static, hand-configured clients in the realm JSON. DCR is for **external** clients we don't control — our own apps don't need it.
