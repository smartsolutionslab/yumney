#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# setup-azure-oidc.sh — One-time Azure OIDC setup for GitHub Actions deployment
#
# Creates:
#   - Two resource groups (staging + production)
#   - One App Registration with Service Principal
#   - Three federated identity credentials (staging, production, dynamic)
#   - Contributor role scoped to each resource group + subscription (for dynamic envs)
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - GitHub repo in the format owner/repo
#
# Usage:
#   ./scripts/setup-azure-oidc.sh <github-org/repo>
#
# Example:
#   ./scripts/setup-azure-oidc.sh heikocodes/yumney
# ==============================================================================

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <github-org/repo>"
  echo "Example: $0 heikocodes/yumney"
  exit 1
fi

GITHUB_REPO="$1"
APP_NAME="yumney-github-deploy"
LOCATION="westeurope"
RG_STAGING="rg-yumney-staging"
RG_PRODUCTION="rg-yumney-production"

echo "=== Yumney — Azure OIDC Setup ==="
echo ""
echo "GitHub Repo:  ${GITHUB_REPO}"
echo "App Name:     ${APP_NAME}"
echo "Location:     ${LOCATION}"
echo ""

# ---------- Resource Groups ----------

echo "--- Creating resource groups ---"

az group create --name "${RG_STAGING}" --location "${LOCATION}" --output none
echo "Created: ${RG_STAGING}"

az group create --name "${RG_PRODUCTION}" --location "${LOCATION}" --output none
echo "Created: ${RG_PRODUCTION}"

# ---------- App Registration + Service Principal ----------

echo ""
echo "--- Creating App Registration ---"

APP_ID=$(az ad app create --display-name "${APP_NAME}" --query appId --output tsv)
echo "App Registration created: ${APP_ID}"

# Create service principal (idempotent — fails gracefully if it exists)
SP_OBJECT_ID=$(az ad sp create --id "${APP_ID}" --query id --output tsv 2>/dev/null || \
  az ad sp show --id "${APP_ID}" --query id --output tsv)
echo "Service Principal: ${SP_OBJECT_ID}"

# ---------- Federated Identity Credentials ----------

echo ""
echo "--- Creating federated identity credentials ---"

# Staging credential (environment-based subject)
az ad app federated-credential create \
  --id "${APP_ID}" \
  --parameters "{
    \"name\": \"github-staging\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_REPO}:environment:staging\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none
echo "Federated credential created: staging"

# Production credential (environment-based subject)
az ad app federated-credential create \
  --id "${APP_ID}" \
  --parameters "{
    \"name\": \"github-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_REPO}:environment:production\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none
echo "Federated credential created: production"

# Dynamic credential (environment-based subject for on-demand environments)
az ad app federated-credential create \
  --id "${APP_ID}" \
  --parameters "{
    \"name\": \"github-dynamic\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_REPO}:environment:dynamic\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none
echo "Federated credential created: dynamic"

# ---------- Role Assignments ----------

echo ""
echo "--- Assigning Contributor role ---"

TENANT_ID=$(az account show --query tenantId --output tsv)
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
STAGING_SCOPE=$(az group show --name "${RG_STAGING}" --query id --output tsv)
PRODUCTION_SCOPE=$(az group show --name "${RG_PRODUCTION}" --query id --output tsv)

az role assignment create \
  --assignee-object-id "${SP_OBJECT_ID}" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "${STAGING_SCOPE}" \
  --output none
echo "Contributor on ${RG_STAGING}"

az role assignment create \
  --assignee-object-id "${SP_OBJECT_ID}" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "${PRODUCTION_SCOPE}" \
  --output none
echo "Contributor on ${RG_PRODUCTION}"

# Subscription-level Contributor (needed for dynamic environments to create/delete resource groups)
SUBSCRIPTION_SCOPE="/subscriptions/${SUBSCRIPTION_ID}"
az role assignment create \
  --assignee-object-id "${SP_OBJECT_ID}" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "${SUBSCRIPTION_SCOPE}" \
  --output none
echo "Contributor on subscription (for dynamic environments)"

# ---------- Output ----------

echo ""
echo "=============================================="
echo "  Setup complete! Configure GitHub environments"
echo "=============================================="
echo ""
echo "Go to: GitHub → Settings → Environments"
echo ""
echo "--- Values for ALL environments ---"
echo "  AZURE_CLIENT_ID:       ${APP_ID}"
echo "  AZURE_TENANT_ID:       ${TENANT_ID}"
echo "  AZURE_SUBSCRIPTION_ID: ${SUBSCRIPTION_ID}"
echo ""
echo "--- Environment: staging ---"
echo "  AZURE_RESOURCE_GROUP:  ${RG_STAGING}"
echo "  AZURE_LOCATION:        ${LOCATION}"
echo "  POSTGRES_USER:         yumneyadmin"
echo "  POSTGRES_PASSWORD:     (generate a strong password)"
echo "  KEYCLOAK_PASSWORD:     (generate a strong password)"
echo "  KEYCLOAK_URL:          (set after first deploy)"
echo "  Deployment branches:   develop only"
echo ""
echo "--- Environment: production ---"
echo "  AZURE_RESOURCE_GROUP:  ${RG_PRODUCTION}"
echo "  AZURE_LOCATION:        ${LOCATION}"
echo "  POSTGRES_USER:         yumneyadmin"
echo "  POSTGRES_PASSWORD:     (generate a strong password)"
echo "  KEYCLOAK_PASSWORD:     (generate a strong password)"
echo "  KEYCLOAK_URL:          (set after first deploy)"
echo "  Deployment branches:   main only, 1 required reviewer"
echo ""
echo "--- Environment: dynamic ---"
echo "  AZURE_CLIENT_ID:       ${APP_ID}"
echo "  AZURE_TENANT_ID:       ${TENANT_ID}"
echo "  AZURE_SUBSCRIPTION_ID: ${SUBSCRIPTION_ID}"
echo "  AZURE_LOCATION:        ${LOCATION}"
echo "  POSTGRES_USER:         yumneyadmin"
echo "  No branch restrictions"
echo "  Passwords are auto-generated and stored in Key Vault per environment"
echo ""
