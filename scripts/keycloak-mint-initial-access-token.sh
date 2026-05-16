#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# keycloak-mint-initial-access-token.sh — Mint a Keycloak RFC 7591 DCR token
#
# MCP clients (Claude.ai HTTP transport, ChatGPT custom GPTs) self-register via
# /realms/yumney/clients-registrations/openid-connect. Anonymous DCR is blocked
# on this realm by an empty trusted-hosts policy — clients must present an
# initial access token (IAT) issued by an admin.
#
# This script mints one IAT and prints it to stdout. Hand the token to the
# client operator out-of-band; they paste it into their MCP-server config and
# the client registers itself on first connect.
#
# Usage:
#   ./scripts/keycloak-mint-initial-access-token.sh [--count N] [--expires SECS]
#
# Env vars:
#   KEYCLOAK_URL      Base URL (default: http://localhost:8080)
#   KEYCLOAK_REALM    Realm (default: yumney)
#   KEYCLOAK_ADMIN    Admin username (default: admin)
#   KEYCLOAK_PASSWORD Admin password (required — do NOT commit)
# ==============================================================================

count=1
expires=86400
while [[ $# -gt 0 ]]; do
  case "$1" in
    --count) count="$2"; shift 2 ;;
    --expires) expires="$2"; shift 2 ;;
    -h|--help) sed -n '4,22p' "$0" ; exit 0 ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

: "${KEYCLOAK_URL:=http://localhost:8080}"
: "${KEYCLOAK_REALM:=yumney}"
: "${KEYCLOAK_ADMIN:=admin}"
: "${KEYCLOAK_PASSWORD:?Set KEYCLOAK_PASSWORD env var (use the AppHost Aspire dashboard secret in dev)}"

admin_token=$(curl --silent --fail \
  --data-urlencode "client_id=admin-cli" \
  --data-urlencode "grant_type=password" \
  --data-urlencode "username=${KEYCLOAK_ADMIN}" \
  --data-urlencode "password=${KEYCLOAK_PASSWORD}" \
  "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)

if [[ -z "$admin_token" ]]; then
  echo "Failed to obtain admin token" >&2
  exit 1
fi

response=$(curl --silent --fail \
  -X POST \
  -H "Authorization: Bearer ${admin_token}" \
  -H "Content-Type: application/json" \
  --data "{\"count\": ${count}, \"expiration\": ${expires}}" \
  "${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/clients-initial-access")

# The response is a single JSON object with token + id + remainingCount fields.
# Extract token without depending on jq — the operator just needs the string.
echo "$response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4
