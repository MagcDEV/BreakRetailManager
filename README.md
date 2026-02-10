# BreakRetailManager

Modular monolith sample with Clean Architecture on .NET 10 (ASP.NET Core) and a Blazor WebAssembly PWA with offline-first capabilities.

## Projects
- `src/Api/BreakRetailManager.Api`: API host for modules, Entra ID JWT auth, and SQL Server persistence.
- `src/Modules/Sales/*`: Sample Sales module (Domain/Application/Infrastructure/Contracts).
- `src/Client/BreakRetailManager.Client`: Blazor WebAssembly PWA with MSAL and offline cache.

## Prerequisites
- .NET 10 SDK
- SQL Server LocalDB or Developer Edition (dev)

## Configure Microsoft Entra ID
1. Register an **API** app.
   - Expose an API scope (e.g., `access_as_user`).
   - Note the **Tenant ID** and **Client ID**.
2. Register a **SPA** app.
   - Add redirect URI: `https://localhost:7270/authentication/login-callback`.
3. Update configuration:
   - API: `src/Api/BreakRetailManager.Api/appsettings.json` (`AzureAd` + connection string).
   - Client: `src/Client/BreakRetailManager.Client/wwwroot/appsettings.json` (`AzureAd` + `Api`).

## Run locally
```bash
dotnet restore --ignore-failed-sources
dotnet run --project src/Api/BreakRetailManager.Api
dotnet run --project src/Client/BreakRetailManager.Client
```

## Offline-first notes
- Orders are cached in IndexedDB and queued offline in an outbox.
- The Sales page can sync queued orders once the client is online again.

## Azure deployment
### API (Azure App Service)
- Publish `src/Api/BreakRetailManager.Api`.
- Configure connection string `DefaultConnection` (Azure SQL).
- Set `AzureAd` settings as App Service configuration.

### Client (Azure Static Web Apps)
- App location: `src/Client/BreakRetailManager.Client`
- Output location: `wwwroot`
- The `staticwebapp.config.json` is already included in the client project.
