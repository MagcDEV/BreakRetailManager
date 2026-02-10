namespace BreakRetailManager.Inventory.Contracts;

public sealed record ProductDto(
    Guid Id,
    string Barcode,
    string Name,
    string Description,
    string Category,
    decimal CostPrice,
    decimal SalePrice,
    int StockQuantity,
    int ReorderLevel,
    bool IsLowStock,
    Guid ProviderId,
    string ProviderName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
