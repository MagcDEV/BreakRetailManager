using System.Text;
using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Domain.Entities;

namespace BreakRetailManager.Inventory.Application;

public static class InventoryMappings
{
    public static ProductDto ToDto(Product product) =>
        new(
            product.Id,
            RepairText(product.Barcode),
            RepairText(product.Name),
            RepairText(product.Description),
            RepairText(product.Category),
            product.CostPrice,
            product.SalePrice,
            product.StockQuantity,
            product.ReorderLevel,
            product.IsLowStock,
            product.ProviderId,
            RepairText(product.Provider?.Name ?? string.Empty),
            product.CreatedAt,
            product.UpdatedAt);

    public static ProviderDto ToDto(Provider provider) =>
        new(
            provider.Id,
            RepairText(provider.Name),
            RepairText(provider.ContactName),
            RepairText(provider.Phone),
            RepairText(provider.Email),
            RepairText(provider.Address),
            provider.CreatedAt);

    public static LocationDto ToDto(Location location) =>
        new(
            location.Id,
            RepairText(location.Name),
            RepairText(location.Address),
            location.IsActive,
            location.CreatedAt);

    public static LocationStockDto ToDto(LocationStock stock) =>
        new(
            stock.Id,
            stock.LocationId,
            RepairText(stock.Location?.Name ?? string.Empty),
            stock.ProductId,
            RepairText(stock.Product?.Name ?? string.Empty),
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

    private static string RepairText(string value)
    {
        if (string.IsNullOrEmpty(value) || (!value.Contains('Ã') && !value.Contains('Â')))
        {
            return value;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 0xFF)
            {
                return value;
            }
        }

        var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
        return repaired.Contains('\uFFFD') ? value : repaired;
    }
}
