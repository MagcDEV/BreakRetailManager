# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, Run, and Test

```bash
# Restore
dotnet restore --ignore-failed-sources

# Build
dotnet build BreakRetailManager.slnx --nologo

# Run API (serves at https://localhost:7138 / http://localhost:5264)
dotnet run --project src/Api/BreakRetailManager.Api

# Run Blazor client
dotnet run --project src/Client/BreakRetailManager.Client

# Run all tests
dotnet test BreakRetailManager.slnx --nologo

# Run a single test project
dotnet test src/Modules/Sales/BreakRetailManager.Sales.Application.Tests/BreakRetailManager.Sales.Application.Tests.csproj --nologo

# Run a single test by name
dotnet test src/Modules/Sales/BreakRetailManager.Sales.Application.Tests/BreakRetailManager.Sales.Application.Tests.csproj --filter "FullyQualifiedName~TestMethodName" --nologo
```

No standalone lint command exists. Use `dotnet build` and `dotnet test` as the validation baseline.

## Architecture

Modular monolith on **.NET 10** with Clean Architecture. Four business modules share one SQL Server database but each has its own EF Core DbContext.

### Project layout

- **`src/Api/BreakRetailManager.Api`** — Composition root. `Program.cs` wires OpenAPI, JWT auth (Microsoft Entra ID), output caching, SignalR, CORS, module discovery, and DB migrations.
- **`src/BuildingBlocks/BreakRetailManager.BuildingBlocks`** — Shared abstractions across modules: `IModule`, pagination types, realtime hub contracts, cross-module interfaces (e.g. `IInventoryStockService`).
- **`src/Modules/{Sales,Inventory,UserManagement,AccountsControl}`** — Each module has five projects:
  - `Domain` — Entities and invariants (zero external dependencies)
  - `Application` — Services, repository interfaces, DTOs, mapping helpers
  - `Infrastructure` — EF Core DbContext, repositories, module registration (`IModule` impl), Minimal API endpoint definitions
  - `Contracts` — Request/response DTOs shared with API and client
  - `Application.Tests` — xUnit tests (Sales and AccountsControl have these)
- **`src/Client/BreakRetailManager.Client`** — Blazor WebAssembly PWA. References only `Contracts` projects. Authenticates via MSAL redirect mode.

### Module system

Modules are discovered via `AddModules(...)` in `ModuleExtensions`. Each module implements `IModule` in its Infrastructure project, self-registering services and endpoints. The API host calls `MapModules()` to wire all endpoints under `/api/{module}` route groups.

### Key cross-cutting concerns

- **Auth**: JWT Bearer on API (Microsoft Identity Web), MSAL on client. Roles (Admin, Manager, Cashier) are database-managed via `LocalRoleClaimsTransformation`, not Entra ID claims. `GET /api/users/me` auto-provisions users; first user becomes Admin.
- **Authorization policies**: `Admin` (Admin only), `Manager` (Admin + Manager), `Cashier` (Admin + Manager + Cashier). Applied per-endpoint, not globally.
- **Output caching**: Three policies — "Short" (30s), "Medium" (60s), "Long" (120s).
- **DB migrations**: Run in fixed order in dev (Sales -> Users -> Inventory -> AccountsControl) because inventory migrations include fixes that touch sales tables.
- **Stock concurrency**: `LocationStock` uses `RowVersion` for optimistic concurrency with up to 3 retries on `DbUpdateConcurrencyException`.
- **Offline-first sales**: Client writes orders to IndexedDB outbox when offline/API fails. `SyncOutboxAsync` replays queued orders oldest-first on reconnect.
- **Realtime inventory**: SignalR hub at `/inventory-hub` (requires Manager policy). Location-based group broadcasting.
- **Fiscal integration (ARCA)**: In Sales Infrastructure. Certificate source is configuration-driven (file or Key Vault).

### Strict module boundaries

Cross-module behavior goes through interfaces in `BuildingBlocks`, never direct references between modules. Example: Sales depends on `IInventoryStockService` (defined in BuildingBlocks), which Inventory implements.

The client must only reference `Contracts` projects — never Application or Infrastructure.

## Configuration

- API config: `src/Api/BreakRetailManager.Api/appsettings*.json` (AzureAd, ConnectionStrings, CORS origins, ARCA settings)
- Client config: `src/Client/BreakRetailManager.Client/wwwroot/appsettings*.json` (Azure AD authority, API base URL, scopes)
- Dev DB: default points to Azure SQL with `Authentication=Active Directory Default`. Override with `ConnectionStrings__DefaultConnection` env var or user secrets for LocalDB.

## CI/CD

GitHub Actions deploys on push to `main`:
- `deploy-api-appservice.yml` — API to Azure App Service
- `deploy-client-staticwebapp.yml` — Client to Azure Static Web Apps

## Skills

- **`/brm-azure`** — Azure resources, CI/CD, Key Vault, database, and ops details.
- **`/invite-entra-guest`** — Onboarding external users to the Entra tenant.
