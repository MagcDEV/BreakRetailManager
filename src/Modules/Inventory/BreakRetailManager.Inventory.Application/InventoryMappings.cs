using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Domain.Entities;

namespace BreakRetailManager.Inventory.Application;

public static class InventoryMappings
{
    public static ProductDto ToDto(Product product) =>
        new(
            product.Id,
            product.Barcode,
            product.Name,
            product.Description,
            product.Category,
            product.CostPrice,
            product.SalePrice,
            product.StockQuantity,
            product.ReorderLevel,
            product.IsLowStock,
            product.ProviderId,
            product.Provider?.Name ?? string.Empty,
            product.CreatedAt,
            product.UpdatedAt);

    public static ProviderDto ToDto(Provider provider) =>
        new(
            provider.Id,
            provider.Name,
            provider.ContactName,
            provider.Phone,
            provider.Email,
            provider.Address,
            provider.CreatedAt);

    public static LocationDto ToDto(Location location) =>
        new(
            location.Id,
            location.Name,
            location.Address,
            location.IsActive,
            location.CreatedAt);

    public static LocationStockDto ToDto(LocationStock stock) =>
        new(
            stock.Id,
            stock.LocationId,
            stock.Location?.Name ?? string.Empty,
            stock.ProductId,
            stock.Product?.Name ?? string.Empty,
            stock.Quantity,
            stock.ReorderLevel,
            stock.IsLowStock);

    public static Product ToProduct(CreateProductRequest request) =>
        new(
            request.Barcode,
            request.Name,
            request.Description,
            request.Category,
            request.CostPrice,
            request.SalePrice,
            0,
            request.ReorderLevel,
            request.ProviderId);

    public static Provider ToProvider(CreateProviderRequest request) =>
        new(
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address);
}
