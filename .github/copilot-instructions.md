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

## Skills

Azure-specific knowledge and operational runbooks have been moved to dedicated skills in `.github/skills/`:

- **`/brm-azure`** — Azure resources, CI/CD, Key Vault, database, and ops commands.
- **`/invite-entra-guest`** — Invite an external user to the Entra ID tenant via Azure CLI.
