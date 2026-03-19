# BreakRetailManager Copilot Instructions

## Solution context
- Modular monolith on **.NET 10** with an ASP.NET Core API and a **Blazor WebAssembly PWA** client.
- Deployment targets are **Azure App Service** for the API and **Azure Static Web Apps** for the client.
- The application uses **SQL Server** and **Microsoft Entra ID** auth: MSAL on the client, JWT Bearer on the API.
- There is no Docker workflow in this repository.

## Build, run, and test commands
- Restore dependencies: `dotnet restore --ignore-failed-sources`
- Build the full solution: `dotnet build BreakRetailManager.slnx --nologo`
- Run the API locally: `dotnet run --project src/Api/BreakRetailManager.Api`
- Run the client locally: `dotnet run --project src/Client/BreakRetailManager.Client`
- Run all tests: `dotnet test BreakRetailManager.slnx --nologo`
- Run the Sales test project only: `dotnet test src/Modules/Sales/BreakRetailManager.Sales.Application.Tests/BreakRetailManager.Sales.Application.Tests.csproj --nologo`
- Run a single xUnit test: `dotnet test src/Modules/Sales/BreakRetailManager.Sales.Application.Tests/BreakRetailManager.Sales.Application.Tests.csproj --filter "FullyQualifiedName~CreateOrderAsync_AppliesPercentageOfferDiscount" --nologo`
- CI publish commands are driven by the workflows: `dotnet publish src/Api/BreakRetailManager.Api/BreakRetailManager.Api.csproj -c Release` and `dotnet publish src/Client/BreakRetailManager.Client/BreakRetailManager.Client.csproj -c Release`
- There is no standalone lint command configured in the repo. Use `dotnet build` and `dotnet test` as the validation baseline.

## High-level architecture
- `src/Api/BreakRetailManager.Api` is the composition root. `Program.cs` configures OpenAPI, auth, output caching, SignalR, CORS, module loading, and DB migration order.
- `src/BuildingBlocks/BreakRetailManager.BuildingBlocks` holds shared abstractions that modules are allowed to share, including `IModule`, pagination types, realtime hub contracts, and cross-module interfaces such as `IInventoryStockService`.
- Business areas live under `src/Modules/*`. Each module follows the same split:
  - `Domain`: entities and invariants
  - `Application`: services, repository interfaces, mappings, business logic
  - `Infrastructure`: EF Core, repositories, module registration, endpoints, integrations
  - `Contracts`: request/response DTOs shared with the API and client
- `src/Client/BreakRetailManager.Client` is a Blazor WASM PWA that references module `Contracts` projects, authenticates with MSAL, and talks to the API through typed service wrappers.

## Key conventions
- Modules are discovered through `AddModules(...)` in `ModuleExtensions`. Each module provides an `IModule` implementation in its Infrastructure project, and the API host registers modules by assembly.
- Keep module boundaries strict. Cross-module behavior goes through interfaces in `BuildingBlocks`, not direct references between module projects. The main example is Sales depending on `IInventoryStockService`, which Inventory implements.
- Keep request/response DTOs in `Contracts` and mapping helpers in `Application` (`SalesMappings`, `OfferMappings`, `InventoryMappings`, `UserMappings`).
- The client should continue to reference only `Contracts` projects. Do not pull Application or Infrastructure code into the Blazor client.
- All module DbContexts share the same SQL Server database. The API host migrates them in a fixed order during development because inventory migrations currently include fixes that touch sales tables.
- User roles are database-managed, not taken directly from Entra ID. `LocalRoleClaimsTransformation` adds local role claims after authentication, and `GET /api/users/me` auto-provisions the current user if needed. The first user created becomes `Admin`.
- Sales stock changes happen server-side during order creation. `SalesOrderService.CreateOrderAsync` calls `IInventoryStockService.DecrementStockForSaleAsync` before persisting the order.
- Inventory stock concurrency is optimistic. `LocationStock` has a `RowVersion`, and `InventoryStockService` retries `DbUpdateConcurrencyException` up to 3 times before giving up.
- Minimal API endpoints are declared inside each module's Infrastructure project under `/api/{module}` route groups, with authorization and output-cache policies applied there instead of in the API host.
- The client is intentionally offline-first for sales flows. `SalesApiClient` writes orders to IndexedDB-backed outbox storage when offline or when API calls fail, and `SyncOutboxAsync` replays queued orders oldest-first when connectivity returns.
- Preserve the PWA/MSAL wiring in the client: `AddMsalAuthentication(...)` in `Program.cs`, the service worker settings in the client `.csproj`, `_content/Microsoft.Authentication.WebAssembly.Msal/AuthenticationService.js` in `wwwroot/index.html`, and service worker registration with `updateViaCache: 'none'`.
- Inventory realtime updates use SignalR through `InventoryHub` at `/inventory-hub`, and the hub is protected with the `Manager` policy.

## Platform and configuration
- API configuration lives in `src/Api/BreakRetailManager.Api/appsettings*.json`.
- Client auth/API settings live in `src/Client/BreakRetailManager.Client/wwwroot/appsettings*.json`.
- The checked-in dev setup expects either LocalDB/Developer Edition or Azure SQL with Entra auth. The README notes that the default API dev config targets Azure SQL with `Authentication=Active Directory Default`.
- CORS allowed origins come from `Client:AllowedOrigins` in API configuration.
- The Sales infrastructure project contains the ARCA fiscal integration. Certificate source selection is configuration-driven and switches between file-based and Key Vault certificate providers.

## Skills
- Use **`/brm-azure`** for Azure resources, CI/CD, Key Vault, database, and ops details that are intentionally kept out of the main instructions file.
- Use **`/invite-entra-guest`** when the task is to onboard an external user into the Entra tenant.
