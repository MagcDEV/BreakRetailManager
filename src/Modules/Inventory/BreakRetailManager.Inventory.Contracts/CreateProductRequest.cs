namespace BreakRetailManager.Inventory.Contracts;

public sealed record CreateProductRequest(
    string Barcode,
    string Name,
    string Description,
    string Category,
    decimal CostPrice,
    decimal SalePrice,
    int ReorderLevel,
    Guid ProviderId);
