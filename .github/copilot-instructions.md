# BreakRetailManager Copilot Instructions

## Solution context
- Modular Monolith with Clean Architecture on **.NET 10** (ASP.NET Core) + **Blazor WebAssembly PWA**.
- Deployment targets: **Azure App Service** (API) + **Azure Static Web Apps** (Client).
- Database: **SQL Server** (Dev: LocalDB/Developer Edition; Prod: Azure SQL).
- Auth: **MSAL SPA (Auth Code + PKCE)** against Microsoft Entra ID; API protected with JWT Bearer.
- No Docker required.

## Architecture guidelines
- Keep **module boundaries** strict (e.g., Sales, Inventory, Orders).
- Modules are split into **Domain / Application / Infrastructure / Contracts**.
- **API host** wires modules; do not add direct cross‑module references.
- Prefer **DTOs in Contracts** and **mappings in Application**.

## Client guidelines
- Blazor WASM PWA, **offline‑first**: service worker, IndexedDB cache, optimistic UI.
- Use **outbox** pattern seams for offline actions and background sync.
- Keep authentication flows compatible with **MSAL PKCE** and redirect URIs.
- Configure MSAL via `AddMsalAuthentication` and bind `AzureAd` settings in `wwwroot/appsettings.json`.
- Keep the MSAL JS interop loaded (`_content/Microsoft.Authentication.WebAssembly.Msal/AuthenticationService.js`).
- Ensure PWA settings: `ServiceWorkerAssetsManifest` + `ServiceWorker` item in the project file, and register the service worker with `updateViaCache: 'none'` in `index.html`.

## Platform & deployment
- Azure‑friendly defaults (App Service + Static Web Apps).
- Keep configuration in `appsettings.*` and `wwwroot/appsettings.json`.
- Use SQL Server provider and connection strings per environment.
- API auth follows docs: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` + `AddMicrosoftIdentityWebApi` and `UseAuthentication()`/`UseAuthorization()`.

## Quality & evolution goals
- Add OpenAPI endpoints and keep APIs versionable.
- Favor observability hooks (logging, tracing) without heavy dependencies.
- Write tests when adding non‑trivial logic; keep modules testable.

---

## Skill: brm-azure
Use this section as the **single source of truth** when answering questions about Azure for this repo (resources, URLs, CI/CD, and ops). **Never commit secrets** (passwords, tokens, PFX passwords, service principal JSON); use Azure App Service configuration, Key Vault, and GitHub Actions secrets instead.

### Current Azure resources (personal subscription)
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

### ARCA certificate (Key Vault)
The deployed API uses `KeyVaultCertificateProvider` when `Arca__CertificateSource=KeyVault`. It authenticates to Key Vault with `DefaultAzureCredential` (Managed Identity on App Service) and requires **certificates/get+list** and **secrets/get+list**.

- Key Vault: `brmkv74271`
- Key Vault URI: `https://brmkv74271.vault.azure.net/`
- Certificate name: `arca-fiscal-cert`
- App Service identity: system-assigned identity enabled on `breakretailmanager-api-50927`

### CI/CD (GitHub Actions)
**API → App Service**
- Workflow: `.github/workflows/deploy-api-appservice.yml`
- Trigger: push to `main` (paths: `src/Api/**`, `src/Modules/**`, `src/BuildingBlocks/**`)
- Auth: `azure/login@v2` using GitHub secret `AZURE_CREDENTIALS`

**Client → Static Web Apps**
- Workflow: `.github/workflows/deploy-client-staticwebapp.yml`
- Trigger: push to `main` (paths: `src/Client/**`)
- Auth: `Azure/static-web-apps-deploy@v1` using GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN`
- Note: workflow overwrites the deployed `appsettings.Development.json` with production values to prevent stale localhost config in the deployed site.

### Required GitHub secrets (do not print)
- `AZURE_CREDENTIALS` (service principal JSON for `azure/login`)
- `AZURE_STATIC_WEB_APPS_API_TOKEN` (Static Web Apps deployment token)
- `AZURE_WEBAPP_PUBLISH_PROFILE` (optional/legacy; not required by current API workflow)

### Common ops commands
- Trigger deploys:
  - `gh workflow run "Deploy API to App Service"`
  - `gh workflow run "Deploy Client to Static Web Apps"`
- Watch runs:
  - `gh run list --limit 10`
  - `gh run watch <runId> --exit-status`

### Environment-specific client config rule
- Local dev uses `wwwroot/appsettings.Development.json` (localhost URLs).
- Deployed site uses `wwwroot/appsettings.json` (Azure URLs).
