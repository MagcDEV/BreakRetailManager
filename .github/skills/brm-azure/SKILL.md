---
name: brm-azure
description: Single source of truth for Azure resources, URLs, CI/CD, and ops for the BreakRetailManager repo. Use this when answering questions about Azure infrastructure, deployments, Key Vault, database, or CI/CD pipelines.
---

**Never commit secrets** (passwords, tokens, PFX passwords, service principal JSON); use Azure App Service configuration, Key Vault, and GitHub Actions secrets instead.

## Current Azure resources (personal subscription)

**Resource group**
- Name: `BreakRetailManager-rg`
- Region: `Central US`

**Backend (API)**
- App Service plan (Free): `brm-api-plan-f1` (SKU `F1`)
- Web App: `breakretailmanager-api-50927`
- Base URL: `https://breakretailmanager-api-50927.azurewebsites.net`
- Health: `GET /health`

**Frontend (Blazor WASM)**
- Static Web App (Free): `breakretailmanager-spa-50927`
- URL: `https://brave-island-07df6c910.1.azurestaticapps.net`
- IMPORTANT: **Do not access** `https://brave-island-07df6c910.1.azurestaticapps.net` (or fetch any resources from it) under any circumstance.
- Deployed config: `GET /appsettings.json`

**Database (Azure SQL)**
- Server: `brmsql42014.database.windows.net`
- DB: `BreakRetailManagerDb`
- Admin login (no password in repo): `brmadmin87117`
- Firewall baseline: `AllowAzureServices (0.0.0.0)` (allows Azure services)
- To allow SSMS from your current IP:
  - `az sql server firewall-rule create -g BreakRetailManager-rg -s brmsql42014 -n AllowSSMS_<ip> --start-ip-address <ip> --end-ip-address <ip>`

## ARCA certificate (Key Vault)

The deployed API uses `KeyVaultCertificateProvider` when `Arca__CertificateSource=KeyVault`. It authenticates to Key Vault with `DefaultAzureCredential` (Managed Identity on App Service) and requires **certificates/get+list** and **secrets/get+list**.

- Key Vault: `brmkv74271`
- Key Vault URI: `https://brmkv74271.vault.azure.net/`
- Certificate name: `arca-fiscal-cert`
- App Service identity: system-assigned identity enabled on `breakretailmanager-api-50927`

## CI/CD (GitHub Actions)

**API → App Service**
- Workflow: `.github/workflows/deploy-api-appservice.yml`
- Trigger: push to `main` (paths: `src/Api/**`, `src/Modules/**`, `src/BuildingBlocks/**`)
- Auth: `azure/login@v2` using GitHub secret `AZURE_CREDENTIALS`

**Client → Static Web Apps**
- Workflow: `.github/workflows/deploy-client-staticwebapp.yml`
- Trigger: push to `main` (paths: `src/Client/**`)
- Auth: `Azure/static-web-apps-deploy@v1` using GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN`
- Note: workflow overwrites the deployed `appsettings.Development.json` with production values to prevent stale localhost config in the deployed site.

## Required GitHub secrets (do not print)

- `AZURE_CREDENTIALS` (service principal JSON for `azure/login`)
- `AZURE_STATIC_WEB_APPS_API_TOKEN` (Static Web Apps deployment token)
- `AZURE_WEBAPP_PUBLISH_PROFILE` (optional/legacy; not required by current API workflow)

## Common ops commands

- Trigger deploys:
  - `gh workflow run "Deploy API to App Service"`
  - `gh workflow run "Deploy Client to Static Web Apps"`
- Watch runs:
  - `gh run list --limit 10`
  - `gh run watch <runId> --exit-status`

## Environment-specific client config rule

- Local dev uses `wwwroot/appsettings.Development.json` (localhost URLs).
- Deployed site uses `wwwroot/appsettings.json` (Azure URLs).
