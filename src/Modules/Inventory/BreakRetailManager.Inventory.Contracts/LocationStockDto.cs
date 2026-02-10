namespace BreakRetailManager.Inventory.Contracts;

public sealed record LocationStockDto(
    Guid Id,
    Guid LocationId,
    string LocationName,
    Guid ProductId,
    string ProductName,
    int Quantity,
    int ReorderLevel,
    bool IsLowStock);
