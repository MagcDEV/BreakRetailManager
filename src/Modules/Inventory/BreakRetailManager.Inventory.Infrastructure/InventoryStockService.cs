using BreakRetailManager.BuildingBlocks.Inventory;
using BreakRetailManager.BuildingBlocks.Realtime;
using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure;

public sealed class InventoryStockService : IInventoryStockService
{
    private readonly InventoryDbContext _dbContext;
    private readonly ILocationStockRepository _locationStockRepository;
    private readonly IHubContext<InventoryHub> _hubContext;

    private const int MaxRetries = 3;

    public InventoryStockService(
        InventoryDbContext dbContext,
        ILocationStockRepository locationStockRepository,
        IHubContext<InventoryHub> hubContext)
    {
        _dbContext = dbContext;
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

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var productIds = items.Select(i => i.ProductId).Distinct().ToList();

                // Batch-read all required LocationStock rows
                var stocks = await _locationStockRepository.GetByLocationAndProductsAsync(
                    locationId, productIds, cancellationToken);
                var stockMap = stocks.ToDictionary(s => s.ProductId);

                // Apply all mutations in memory
                events.Clear();
                foreach (var item in items)
                {
                    if (!stockMap.TryGetValue(item.ProductId, out var stock))
                    {
                        throw new InvalidOperationException(
                            $"No stock record exists for product {item.ProductId} at location {locationId}.");
                    }

                    stock.UpdateQuantity(-item.Quantity);

                    events.Add(new InventoryStockChangedEvent(
                        stock.ProductId, stock.LocationId, stock.Quantity, DateTimeOffset.UtcNow));
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                foreach (var entry in _dbContext.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
                events.Clear();
            }
        }

        // Broadcast SignalR events after successful commit
        foreach (var evt in events)
        {
            await _hubContext.Clients.Group(evt.LocationId.ToString()).SendAsync(
                InventoryHub.StockChangedMethod, evt, cancellationToken);
        }
    }
}
