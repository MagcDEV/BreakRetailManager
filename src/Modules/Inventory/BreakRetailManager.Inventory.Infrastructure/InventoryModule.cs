using BreakRetailManager.BuildingBlocks.Modules;
using BreakRetailManager.BuildingBlocks.Realtime;
using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BreakRetailManager.Inventory.Infrastructure;

public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProviderService>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ILocationStockRepository, LocationStockRepository>();
        services.AddScoped<LocationService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization("Cashier");

        // Manager-only endpoints (Cashier policy includes Manager/Admin, so adding Manager here keeps access restricted)
        var managerGroup = group.MapGroup(string.Empty)
            .RequireAuthorization("Manager");

        // Product endpoints
        managerGroup.MapGet("/products", async (ProductService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetProductsAsync(cancellationToken)))
            .Produces<IReadOnlyList<ProductDto>>();

        managerGroup.MapGet("/products/low-stock", async (ProductService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetLowStockProductsAsync(cancellationToken)))
            .Produces<IReadOnlyList<ProductDto>>();

        managerGroup.MapGet("/products/{id:guid}", async (Guid id, ProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetProductByIdAsync(id, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }).Produces<ProductDto>();

        group.MapGet("/products/barcode/{barcode}", async (string barcode, ProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetProductByBarcodeAsync(barcode, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }).Produces<ProductDto>()
          .RequireAuthorization("Cashier");

        managerGroup.MapPost("/products", async (CreateProductRequest request, ProductService service, CancellationToken cancellationToken) =>
            Results.Created($"/api/inventory/products", await service.CreateProductAsync(request, cancellationToken)))
            .Produces<ProductDto>(StatusCodes.Status201Created);

        managerGroup.MapPut("/products/{id:guid}", async (Guid id, UpdateProductRequest request, ProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.UpdateProductAsync(id, request, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }).Produces<ProductDto>();

        // Provider endpoints
        managerGroup.MapGet("/providers", async (ProviderService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetProvidersAsync(cancellationToken)))
            .Produces<IReadOnlyList<ProviderDto>>();

        managerGroup.MapGet("/providers/{id:guid}", async (Guid id, ProviderService service, CancellationToken cancellationToken) =>
        {
            var provider = await service.GetProviderByIdAsync(id, cancellationToken);
            return provider is null ? Results.NotFound() : Results.Ok(provider);
        }).Produces<ProviderDto>();

        managerGroup.MapPost("/providers", async (CreateProviderRequest request, ProviderService service, CancellationToken cancellationToken) =>
            Results.Created($"/api/inventory/providers", await service.CreateProviderAsync(request, cancellationToken)))
            .Produces<ProviderDto>(StatusCodes.Status201Created);

        managerGroup.MapPut("/providers/{id:guid}", async (Guid id, UpdateProviderRequest request, ProviderService service, CancellationToken cancellationToken) =>
        {
            var provider = await service.UpdateProviderAsync(id, request, cancellationToken);
            return provider is null ? Results.NotFound() : Results.Ok(provider);
        }).Produces<ProviderDto>();

        // Location endpoints
        group.MapGet("/locations", async (LocationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetLocationsAsync(cancellationToken)))
            .Produces<IReadOnlyList<LocationDto>>()
            .RequireAuthorization("Cashier");

        managerGroup.MapGet("/locations/{id:guid}", async (Guid id, LocationService service, CancellationToken cancellationToken) =>
        {
            var location = await service.GetLocationByIdAsync(id, cancellationToken);
            return location is null ? Results.NotFound() : Results.Ok(location);
        }).Produces<LocationDto>();

        managerGroup.MapPost("/locations", async (CreateLocationRequest request, LocationService service, CancellationToken cancellationToken) =>
            Results.Created($"/api/inventory/locations", await service.CreateLocationAsync(request, cancellationToken)))
            .Produces<LocationDto>(StatusCodes.Status201Created);

        // Location stock endpoints
        group.MapGet("/locations/{locationId:guid}/stock", async (Guid locationId, ProductService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetStockByLocationAsync(locationId, cancellationToken)))
            .Produces<IReadOnlyList<LocationStockDto>>()
            .RequireAuthorization("Cashier");

        // Sale-driven stock decrements — Cashier and above (negative deltas only)
        group.MapPatch("/locations/{locationId:guid}/stock/{productId:guid}/sale", async (
            Guid locationId,
            Guid productId,
            StockUpdateRequest request,
            ProductService service,
            IHubContext<InventoryHub> hubContext,
            CancellationToken cancellationToken) =>
        {
            if (request.Quantity >= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["quantity"] = new[] { "Sale stock adjustments must be a negative value." }
                });
            }

            try
            {
                var stock = await service.UpdateLocationStockAsync(locationId, productId, request.Quantity, cancellationToken);
                if (stock is null)
                {
                    return Results.NotFound();
                }

                await hubContext.Clients.All.SendAsync(
                    InventoryHub.StockChangedMethod,
                    new InventoryStockChangedEvent(stock.ProductId, stock.LocationId, stock.Quantity, DateTimeOffset.UtcNow),
                    cancellationToken);

                return Results.Ok(stock);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["quantity"] = new[] { ex.Message }
                });
            }
        })
        .Produces<LocationStockDto>()
        .ProducesValidationProblem(400)
        .RequireAuthorization("Cashier");

        managerGroup.MapPatch("/locations/{locationId:guid}/stock/{productId:guid}", async (
            Guid locationId,
            Guid productId,
            StockUpdateRequest request,
            ProductService service,
            IHubContext<InventoryHub> hubContext,
            CancellationToken cancellationToken) =>
        {
            var stock = await service.UpdateLocationStockAsync(locationId, productId, request.Quantity, cancellationToken);
            if (stock is null)
            {
                return Results.NotFound();
            }

            await hubContext.Clients.All.SendAsync(
                InventoryHub.StockChangedMethod,
                new InventoryStockChangedEvent(stock.ProductId, stock.LocationId, stock.Quantity, DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Ok(stock);
        }).Produces<LocationStockDto>();
    }
}

public sealed record StockUpdateRequest(int Quantity);
