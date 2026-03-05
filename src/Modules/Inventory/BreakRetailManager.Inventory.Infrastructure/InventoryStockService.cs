using BreakRetailManager.BuildingBlocks.Inventory;
using BreakRetailManager.BuildingBlocks.Realtime;
using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Domain.Entities;
using BreakRetailManager.Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure;

public sealed class InventoryStockService : IInventoryStockService
{
    private readonly InventoryDbContext _dbContext;
    private readonly IProductRepository _productRepository;
    private readonly ILocationStockRepository _locationStockRepository;
    private readonly IHubContext<InventoryHub> _hubContext;

    private const int MaxRetries = 3;

    public InventoryStockService(
        InventoryDbContext dbContext,
        IProductRepository productRepository,
        ILocationStockRepository locationStockRepository,
        IHubContext<InventoryHub> hubContext)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _locationStockRepository = locationStockRepository;
        _hubContext = hubContext;
    }

    public async Task DecrementStockForSaleAsync(
        Guid locationId,
        IReadOnlyList<SaleStockItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return;

        var events = new List<InventoryStockChangedEvent>(items.Count);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in items)
        {
            var stock = await DecrementSingleItemAsync(locationId, item.ProductId, item.Quantity, cancellationToken);
            events.Add(new InventoryStockChangedEvent(
                stock.ProductId, stock.LocationId, stock.Quantity, DateTimeOffset.UtcNow));
        }

        await transaction.CommitAsync(cancellationToken);

        // Broadcast SignalR events after successful commit
        foreach (var evt in events)
        {
            await _hubContext.Clients.All.SendAsync(
                InventoryHub.StockChangedMethod, evt, cancellationToken);
        }
    }

    private async Task<LocationStock> DecrementSingleItemAsync(
        Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var stock = await _locationStockRepository.GetAsync(locationId, productId, cancellationToken);

                if (stock is null)
                {
                    throw new InvalidOperationException(
                        $"No stock record exists for product {productId} at location {locationId}.");
                }

                stock.UpdateQuantity(-quantity);

                // Recompute global aggregate from DB
                var total = await _locationStockRepository.GetTotalForProductAsync(productId, cancellationToken);
                total -= quantity; // Adjust for in-flight delta

                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                product?.SetStockQuantity(total);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return stock;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                // Detach tracked entities so the next iteration gets fresh data
                foreach (var entry in _dbContext.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to decrement stock for product {productId} after {MaxRetries} retries due to concurrent updates.");
    }
}
