namespace BreakRetailManager.Inventory.Contracts;

public sealed record UpdateProductRequest(
    string Barcode,
    string Name,
    string Description,
    string Category,
    decimal CostPrice,
    decimal SalePrice,
    int ReorderLevel,
    Guid ProviderId);
