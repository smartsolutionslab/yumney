# Staging Reset (from-scratch redeploy)

When staging gets into a state that's faster to nuke than to fix — stuck Postgres RP, zombie resources from a topology change, surprise mis-configurations — this runbook walks through wiping `rg-yumney-staging` and letting `aspire deploy` recreate it from scratch.

Allow ~30 min end-to-end: RG delete (15 min, CAE long-pole) + deploy run (12 min) + post-deploy fixups.

## When to use this

Reach for it when targeted fixes have failed. Typical triggers:

- Postgres Flexible Server stuck in a non-`Ready` state that `restart` can't recover (see `feedback_aspire_deploy_no_gc` / ADR notes on stuck RP state).
- Accumulated zombies from `aspire deploy`'s no-GC behaviour after the AppHost topology shrinks (orphaned identities, retired Container Apps).
- Key Vault, Container App secrets, or storage accounts that have drifted from what the AppHost expects.

Don't use it for routine code changes. The bypass cost (downtime + reset of any data in staging DBs) only pays off when surgical repair is genuinely harder than redeploy.

## Pre-flight checks

```bash
az login --use-device-code              # if your CLI session is stale
gh auth status                          # need a token that can push to develop
az group show -n rg-yumney-staging -o table
```

Confirm with a teammate before proceeding — staging being down for 30 min surprises the e2e suite and anyone clicking through the deployed app.

## Step 1 — delete the resource group

```bash
az group delete -n rg-yumney-staging --yes --no-wait
```

Poll until gone (CAE is the long-pole; everything else falls in 1–2 min):

```bash
until [ "$(az group exists -n rg-yumney-staging)" = "false" ]; do sleep 30; done
```

## Step 2 — purge soft-deleted Key Vault

Aspire generates a **deterministic** resource-name suffix (e.g. `s4o3y2r2yamcs`) based on the subscription + RG, so the next `aspire deploy` will try to create `postgreskv-<same-suffix>` again. Without a purge, the freshly-deleted vault sits in 90-day soft-delete and the new create collides.

```bash
az keyvault list-deleted --query "[?starts_with(name,'postgreskv')]" -o table
az keyvault purge --name postgreskv-<suffix> --location canadacentral
```

## Step 3 — refresh stale CAE-domain repo vars

The Container Apps Environment gets a fresh random domain prefix on each create (e.g. `calmsky-ae1ea5be` → `wittyglacier-27fd5013`). Two repo vars carry this domain and are baked into both the frontend `app-config.json` and the Keycloak realm JSON at deploy time. **Stale values silently break auth flow** — login pages 404 because the Keycloak URL is wrong; OIDC redirects fail because the gateway URL is wrong.

This step has to wait until **after** the deploy starts and the new CAE is created — until then the new FQDNs don't exist yet. In practice: trigger the deploy first (next step), then while it's running, after `Deploy to Azure` finishes:

```bash
GW=$(az containerapp show -g rg-yumney-staging -n yumney-gateway --query properties.configuration.ingress.fqdn -o tsv)
KC=$(az containerapp show -g rg-yumney-staging -n keycloak --query properties.configuration.ingress.fqdn -o tsv)
gh variable set GATEWAY_URL  -R smartsolutionslab/yumney -b "https://$GW"
gh variable set KEYCLOAK_URL -R smartsolutionslab/yumney -b "https://$KC"
```

The first deploy will ship with the *old* var values and the auth flow will be broken. Re-trigger a second deploy (any push to `develop`, even an empty commit) for the corrected URLs to flow into the frontend container and realm JSON.

## Step 4 — trigger the deploy

The Deploy workflow has no `workflow_dispatch`, so push an empty commit:

```bash
git checkout develop
git pull --ff-only
git commit --allow-empty -m "chore(infra): trigger fresh staging deploy after RG wipe"
git push origin develop
```

Watch with `gh run watch <id>`. Run details: [`.github/workflows/deploy.yml`](../../.github/workflows/deploy.yml). Notable steps:

- `Diagnose Postgres health` — has an `az group exists` guard so it skips cleanly on a fresh RG.
- `Deploy to Azure` — `aspire deploy`, ~5 min.
- `Seed Keycloak realm to file share` — uploads `Realms/yumney-realm.json` into the `bm0` Azure File volume, restarts Keycloak so `--import-realm` runs. Without this, only the `master` realm initializes.
- `Run EF migrations` — invokes the `yumney-migrations` Container Apps Job.
- `Verify Postgres state` — fails the run loudly if any expected DB is missing.

## Step 5 — fix the Keycloak `yumney-api` client secret

> This is a known gap — see [project_keycloak_client_secret_mismatch](https://github.com/smartsolutionslab/yumney/issues?q=keycloak+client+secret) in the team memory.

The imported realm has `dev-only-keycloak-client-secret` literally baked into `Realms/yumney-realm.json`, but the Users API reads `Keycloak__ClientSecret` from the `secrets.YUMNEY_API_CLIENT_SECRET` GH secret (staging value: `yumney-api-secret`). The deploy workflow's `sed` step injects `__GATEWAY_URL__` but has no equivalent for the client secret. Keycloak's `IGNORE_EXISTING` strategy means re-importing the realm post-fix is a no-op — you have to update the client via admin API.

Symptoms: `POST /api/v1/auth/register` returns 503; logs on `users-api` show `Failed to obtain service account token. Status: Unauthorized` and Keycloak returns 401 on the `client_credentials` grant.

Fix:

```bash
KC=https://$(az containerapp show -g rg-yumney-staging -n keycloak --query properties.configuration.ingress.fqdn -o tsv)
ADMIN_PASS=$(az containerapp secret show -g rg-yumney-staging -n keycloak --secret-name kc-bootstrap-admin-password --query value -o tsv)
API_SECRET=$(az containerapp secret show -g rg-yumney-staging -n users-api --secret-name keycloak--clientsecret --query value -o tsv)

# 1. Master admin token
TOKEN=$(curl -sS -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d "grant_type=password&client_id=admin-cli&username=admin&password=$ADMIN_PASS" \
  | jq -r .access_token)

# 2. Internal client id for yumney-api
CLIENT_ID=$(curl -sS -H "Authorization: Bearer $TOKEN" \
  "$KC/admin/realms/yumney/clients?clientId=yumney-api" | jq -r '.[0].id')

# 3. Set the secret on the client to match what the Users API is sending
curl -sS -X PUT "$KC/admin/realms/yumney/clients/$CLIENT_ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d "{\"secret\":\"$API_SECRET\"}" -w "HTTP %{http_code}\n"   # expect 204
```

Verify with a `client_credentials` grant (should return a token):

```bash
curl -sS -X POST "$KC/realms/yumney/protocol/openid-connect/token" \
  -d "grant_type=client_credentials&client_id=yumney-api&client_secret=$API_SECRET" \
  -w "\nHTTP %{http_code}\n"
```

Then test registration end-to-end:

```bash
GW=https://$(az containerapp show -g rg-yumney-staging -n yumney-gateway --query properties.configuration.ingress.fqdn -o tsv)
curl -sS -X POST "$GW/api/v1/auth/register" -H "Content-Type: application/json" \
  -d "{\"email\":\"smoke+$(date +%s)@yumney.dev\",\"password\":\"Smoke1234!\",\"displayName\":\"Smoke\"}" \
  -w "\nHTTP %{http_code}\n"   # expect 201
```

## Step 6 — end-to-end smoke

Drive the staging frontend in a browser (or `curl` the gateway) and confirm:

- `GET /` returns the shell with title `Sign In — Yumney`
- `GET /assets/config/app-config.json` shows the **new** Keycloak URL (not the stale one from before the wipe)
- `GET <keycloak>/realms/yumney/.well-known/openid-configuration` returns 200
- Login via UI works for the seeded `testuser` / `admin` users (passwords in `Realms/yumney-realm.json`)
- A test recipe imports end-to-end (use [Wikipedia Carbonara](https://en.wikipedia.org/wiki/Carbonara) — the MCP setup sample URL was stale until 2026-05-23)

## Known gaps worth a follow-up PR

| Gap | Workaround | Permanent fix |
|---|---|---|
| Repo vars `GATEWAY_URL` / `KEYCLOAK_URL` need manual refresh after wipe | Step 3 above | Read them dynamically at deploy time, e.g. via `az containerapp show` inside the workflow |
| `yumney-api` client secret mismatch | Step 5 above | Add `__YUMNEY_API_CLIENT_SECRET__` placeholder in realm JSON + sed step + post-import admin-API update (since `IGNORE_EXISTING` skips it on re-import) |
| Deploy workflow has no `workflow_dispatch` — needs a push to retrigger | Empty commit | Add a `workflow_dispatch:` trigger to `deploy.yml` |
