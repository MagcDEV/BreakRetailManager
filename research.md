# Performance Research — BreakRetailManager

> **Date:** 2026-03-05
> **Scope:** Full architecture review across API host, 3 modules (Sales, Inventory, UserManagement), Blazor WASM client, and data layer.

---

## Executive Summary

The application is well-structured (modular monolith, clean architecture) but has several low-hanging-fruit performance issues that become increasingly costly as data volume and concurrent users grow. The biggest wins will come from **adding pagination to sales orders**, **eliminating N+1 patterns** (both server-side and client-side HTTP), **adding response caching**, and **parallelizing independent async calls** on the client.

---

## 1. API / Server-Side Issues

### 1.1 No Pagination on Sales Orders (Critical)

**Where:** `SalesOrderRepository.GetAllAsync()` → `SalesModule` GET `/api/sales/orders`
**Problem:** Loads *every* sales order with *every* line into memory, maps them all to DTOs, and returns the full list. As order volume grows (hundreds → thousands), this will degrade linearly in both memory and response time. The Inventory module already has pagination (`GetPagedAsync`), but Sales does not.
**Impact:** High — the orders endpoint is called on every History tab click and after outbox sync.

```
Recommendation:
- Add a GetPagedAsync method to ISalesOrderRepository/SalesOrderRepository
- Add page/pageSize query parameters to GET /api/sales/orders
- Mirror the existing pattern from ProductRepository.GetPagedAsync
```

### 1.2 Sequential Per-Item Stock Decrement (Medium)

**Where:** `InventoryStockService.DecrementStockForSaleAsync()` lines 44-49
**Problem:** For an order with N line items, each item is decremented one-by-one inside a foreach loop. Each iteration performs: 1 read of LocationStock, 1 read of total for product, 1 read of Product, then SaveChanges. That's ~3 DB round-trips × N items, all inside a transaction that holds locks.

```
Recommendation:
- Batch-read all LocationStock rows and Product rows upfront in a single query
- Apply all mutations in memory
- Call SaveChangesAsync once at the end of the transaction
- This reduces N×3 round-trips to ~2 reads + 1 write
```

### 1.3 Redundant Re-Reads After Create/Update (Low)

**Where:** `ProductService.CreateProductAsync()` line 82, `UpdateProductAsync()` line 106
**Problem:** After saving a new or updated Product, the code immediately calls `GetByIdAsync()` again to re-read the same entity from the database. Since EF Core already tracks the entity, this is an unnecessary round-trip.

```
Recommendation:
- After SaveChangesAsync, map the already-tracked entity directly to a DTO
- Eagerly load the Provider navigation on the tracked entity if needed
```

### 1.4 Low-Stock Loads All Products Then Filters In-Memory (Medium)

**Where:** `ProductService.GetLowStockProductsAsync()` lines 66-74
**Problem:** Calls `GetAllAsync()` (loads all products), then `EnrichWithStockTotals` (aggregates LocationStocks for all products), then filters client-side by `StockQuantity <= ReorderLevel`. This materializes the full product catalog even though only a few products may be low on stock.

```
Recommendation:
- Push the low-stock filter down to SQL: join Products with a subquery that aggregates
  LocationStocks grouped by ProductId, and filter WHERE SUM(Quantity) <= ReorderLevel
- Return only matching products from the database
```

### 1.5 No Response Caching or Compression (Medium)

**Where:** `Program.cs` — no `AddResponseCaching`, `AddOutputCache`, or `AddResponseCompression`
**Problem:** Every API request hits the database. Read-heavy endpoints like products, providers, locations, active offers, and roles rarely change but are fetched on every page load by every user.

```
Recommendation:
- Add output caching (OutputCache middleware in .NET 10) with short TTLs for read-heavy endpoints:
  - GET /api/inventory/products → 30s cache
  - GET /api/inventory/locations → 60s cache
  - GET /api/sales/offers/active → 30s cache
  - GET /api/inventory/providers → 60s cache
- Add response compression (Brotli/gzip) for all JSON responses
- Invalidate caches on mutations (POST/PUT/PATCH/DELETE)
```

### 1.6 SignalR Broadcasts to All Clients (Low-Medium)

**Where:** `InventoryStockService` line 56, `InventoryModule` lines 182, 215
**Problem:** `_hubContext.Clients.All.SendAsync(...)` sends every stock change event to every connected client regardless of which location they're viewing. With many concurrent users across many locations, most messages are irrelevant noise.

```
Recommendation:
- Use SignalR Groups, one per location: Clients.Group(locationId.ToString()).SendAsync(...)
- Have clients join the relevant group when they select a location
- This dramatically reduces unnecessary message processing
```

### 1.7 Sequential Migrations on Startup (Low)

**Where:** `Program.cs` lines 50-56
**Problem:** Three `MigrateAsync()` calls run sequentially. Each one opens a connection, checks migration state, and applies pending migrations.

```
Recommendation:
- Run all three MigrateAsync calls concurrently with Task.WhenAll:
  await Task.WhenAll(
      salesDb.Database.MigrateAsync(),
      usersDb.Database.MigrateAsync(),
      inventoryDb.Database.MigrateAsync());
```

### 1.8 Multiple DB Round-Trips in Stock Update (Medium)

**Where:** `ProductService.TryUpdateLocationStockAsync()` lines 131-169
**Problem:** A single stock-adjust operation performs 4-5 DB calls: read LocationStock, (optionally read Product for new stock record), read total for product, read Product again, then save. Each round-trip adds latency, especially to Azure SQL.

```
Recommendation:
- Combine the stock lookup and product lookup into a single query
- Compute the new total using a SQL expression (e.g., raw SQL UPDATE with output)
  or at minimum batch the reads into fewer calls
```

---

## 2. Client / Blazor WASM Issues

### 2.1 N+1 HTTP Calls in LoadLocationStocksAsync (High)

**Where:** `Inventory.razor` lines 523-539
**Problem:** When adjusting stock for a product, the code loops through every location and makes a separate `GET /api/inventory/locations/{id}/stock` call for each one. If there are 5 locations, that's 5 sequential HTTP calls to the API.

```
Recommendation:
- Add a server-side endpoint: GET /api/inventory/products/{productId}/stock
  that returns stock across all locations in a single query
- Or use Task.WhenAll to parallelize the existing calls:
  var tasks = locations.Select(loc => InventoryApi.GetLocationStockAsync(loc.Id));
  var results = await Task.WhenAll(tasks);
```

### 2.2 Full Data Reload After Every Mutation (Medium-High)

**Where:** `Inventory.razor` — `LoadDataAsync()` called at lines 569, 595, 634, 664, 701
**Problem:** After every create/update/stock-adjust, `LoadDataAsync()` reloads ALL products, ALL providers, and ALL locations from the API. This is wasteful: if you updated one product, you don't need to refetch all providers and locations.

```
Recommendation:
- After creating a product: insert the returned DTO into the local list
- After updating a product: replace the matching item in the local list
- After stock adjustment: update only the affected product's stock quantity
- Only do a full reload on initial page load and SignalR reconnection
```

### 2.3 Sequential Initialization in Sales.razor (Medium)

**Where:** `Sales.razor` `OnInitializedAsync()` lines 325-345
**Problem:** Four async operations run sequentially: `InitializeAsync`, connectivity check, `SyncOutboxAsync`, `GetLocationsAsync`, `GetActiveOffersAsync`. The locations and offers fetch are independent and can overlap.

```
Recommendation:
- After the connectivity check, run sync, locations, and offers in parallel:
  await Task.WhenAll(
      SalesApi.SyncOutboxAsync(),
      Task.Run(async () => locations.AddRange(
          (await InventoryApi.GetLocationsAsync()).Where(l => l.IsActive).OrderBy(l => l.Name))),
      Task.Run(async () => activeOffers.AddRange(await SalesApi.GetActiveOffersAsync()))
  );
```

### 2.4 No Client-Side Data Caching for Inventory (Medium)

**Where:** `InventoryApiClient` — no caching layer, unlike `SalesApiClient` which uses IndexedDB
**Problem:** Every navigation to `/inventory` triggers fresh API calls for products, providers, and locations. Products and providers change infrequently; caching them for even 30 seconds would eliminate redundant requests.

```
Recommendation:
- Add an in-memory cache (or IndexedDB-backed cache like SalesApiClient uses)
  for products, providers, and locations
- Use a short TTL (30-60 seconds) or invalidate on mutations
- Serve stale data immediately, refresh in background (stale-while-revalidate)
```

### 2.5 Connectivity Check via JS Interop on Every API Call (Low)

**Where:** `SalesApiClient.GetOrdersAsync()` line 32, `CreateOrderAsync()` line 60
**Problem:** `IsOnlineAsync()` calls into JavaScript via interop on every API call. JS interop has overhead (~1-2ms per call) and the online status rarely changes mid-session.

```
Recommendation:
- Cache the connectivity status with a short TTL (5-10 seconds)
- Or listen for browser online/offline events via JS and push changes to .NET
  instead of polling on every API call
```

### 2.6 Render-Blocking Google Fonts (Low)

**Where:** `wwwroot/index.html` line 12
**Problem:** The Google Fonts CSS `<link>` with `display=swap` is in the `<head>` without `async` or preload. It blocks rendering until the fonts are fetched.

```
Recommendation:
- Use font-display: swap (already present) but preload the font files:
  <link rel="preload" href="..." as="font" type="font/woff2" crossorigin>
- Or self-host the font files to eliminate the Google Fonts DNS + connection round-trip
- Consider inlining the critical CSS subset
```

---

## 3. Database & EF Core Issues

### 3.1 Missing Indexes (Medium)

**Where:** `SalesDbContext`, `InventoryDbContext`
**Problem:** Several frequently-queried columns lack indexes:

| Table | Column(s) | Used By |
|-------|-----------|---------|
| `sales.SalesOrders` | `CreatedAt` | `OrderByDescending(order => order.CreatedAt)` |
| `sales.SalesOrders` | `LocationId` | Future filtering by location |
| `sales.Offers` | `IsActive` | `Where(offer => offer.IsActive)` |
| `inventory.LocationStocks` | `ProductId` (single-column) | `GetTotalForProductAsync`, `GetTotalsByProductAsync` aggregate queries |

The composite unique index on `LocationStocks(LocationId, ProductId)` helps composite lookups but won't efficiently satisfy queries filtered only by `ProductId`.

```
Recommendation:
- Add migrations with explicit index definitions for the columns above
- For SalesOrders, a covering index on (CreatedAt DESC) including relevant columns
  would eliminate the sort cost on the orders listing
```

### 3.2 Cartesian Explosion from Include (Low-Medium)

**Where:** `SalesOrderRepository.GetAllAsync()` — `.Include(order => order.Lines)`
**Problem:** Single-query `Include` joins SalesOrders with SalesOrderLines, duplicating all SalesOrder columns for every line. For an order with 10 lines, each SalesOrder column value is sent 10 times from the database.

```
Recommendation:
- Use AsSplitQuery() for queries that Include owned collections:
  .AsSplitQuery().Include(order => order.Lines)
- This issues 2 separate queries instead of 1 cartesian join
- Especially important when SalesOrders has many columns (fiscal fields, etc.)
```

### 3.3 Three DbContexts on One Connection String (Low)

**Where:** All three module `RegisterServices` methods
**Problem:** All three `DbContext` types share the same connection string and therefore the same ADO.NET connection pool (default: 100 connections). Under load, modules compete for connections. No ability to tune pool size per module.

```
Recommendation:
- For now, this is acceptable for the current scale
- If connection contention appears, configure separate named connection strings
  with different pool sizes (e.g., Inventory gets more connections due to
  high-frequency stock operations)
- Monitor with SQL Server's sys.dm_exec_connections DMV
```

---

## 4. Architecture / Structural Opportunities

### 4.1 Denormalized StockQuantity on Product (Design Debt)

**Where:** `Product.StockQuantity`, `Product.SetStockQuantity()`
**Problem:** `Product.StockQuantity` is a denormalized aggregate of `LocationStocks`. It's updated on every stock change but also recomputed from `LocationStocks` each time. This creates unnecessary writes to the Products table and can drift out of sync.

```
Recommendation:
- Remove the denormalized column and compute stock totals from LocationStocks on-demand
- Or treat it as a materialized cache that's refreshed asynchronously
  (e.g., via a background job or database trigger)
- The EnrichWithStockTotals pattern already computes the real total; the
  Product.StockQuantity write is redundant overhead
```

### 4.2 Monolithic Blazor Components (Maintainability → Rendering)

**Where:** `Sales.razor` (26.5 KB, ~600+ lines), `Inventory.razor` (31.2 KB, ~720+ lines)
**Problem:** Blazor re-renders the entire component tree on `StateHasChanged`. In a large monolithic component, every state change (typing in the barcode field, changing a quantity) triggers a diffing pass on the full DOM.

```
Recommendation:
- Extract logical sections into child components (e.g., BarcodeScannerInput,
  OrderLineList, OfferPreviewPanel, StockAdjustPanel)
- Use @key on list items for efficient diffing
- Use ShouldRender() overrides in child components to skip unnecessary renders
```

---

## 5. Quick Win Summary (by effort / impact)

| # | Optimization | Effort | Impact | Category |
|---|-------------|--------|--------|----------|
| 1 | Paginate GET /api/sales/orders | Low | High | API |
| 2 | Parallelize Sales.razor init (Task.WhenAll) | Low | Medium | Client |
| 3 | Fix N+1 HTTP in LoadLocationStocksAsync | Low | High | Client |
| 4 | Add response compression | Low | Medium | API |
| 5 | Add output caching for read-heavy endpoints | Low-Med | Medium | API |
| 6 | Targeted list updates after mutations | Medium | Medium-High | Client |
| 7 | Batch stock decrement reads | Medium | Medium | API |
| 8 | Add missing database indexes | Low | Medium | Database |
| 9 | Use AsSplitQuery for Include operations | Low | Low-Med | Database |
| 10 | Add client-side caching for inventory data | Medium | Medium | Client |
| 11 | Use SignalR groups instead of broadcast | Medium | Low-Med | API |
| 12 | Remove denormalized Product.StockQuantity | High | Low-Med | Architecture |

---

## 6. What's Already Done Well

- **Optimistic concurrency** with `RowVersion` on `LocationStock` + retry loops — correctly handles concurrent updates.
- **AsNoTracking** used consistently on read-only queries — avoids change tracker overhead.
- **Offline-first pattern** with IndexedDB outbox in the Sales client — good for PWA reliability.
- **Connection resiliency** with `EnableRetryOnFailure()` on all DbContexts.
- **Pagination exists** in the Inventory module (`GetPagedAsync`) — just needs to be adopted more broadly.
- **Debounced real-time refresh** in the Inventory page (300ms debounce) — prevents thrashing.
- **SignalR `WithAutomaticReconnect`** on the client — handles transient network issues gracefully.
