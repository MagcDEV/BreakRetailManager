namespace BreakRetailManager.BuildingBlocks.Inventory;

/// <summary>
/// Cross-module contract for inventory stock operations.
/// Implemented by the Inventory module, consumed by Sales.
/// </summary>
public interface IInventoryStockService
{
    /// <summary>
    /// Atomically decrements stock for multiple products at a given location (e.g. after a sale).
    /// Throws <see cref="InvalidOperationException"/> if any product has insufficient stock.
    /// </summary>
    Task DecrementStockForSaleAsync(
        Guid locationId,
        IReadOnlyList<SaleStockItem> items,
        CancellationToken cancellationToken = default);
}

public sealed record SaleStockItem(Guid ProductId, int Quantity);
