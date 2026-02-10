using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Domain.Entities;

namespace BreakRetailManager.Inventory.Application;

public sealed class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILocationStockRepository _locationStockRepository;

    public ProductService(IProductRepository productRepository, ILocationStockRepository locationStockRepository)
    {
        _productRepository = productRepository;
        _locationStockRepository = locationStockRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : InventoryMappings.ToDto(product);
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByBarcodeAsync(barcode, cancellationToken);
        return product is null ? null : InventoryMappings.ToDto(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetLowStockAsync(cancellationToken);
        return products.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = InventoryMappings.ToProduct(request);
        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        var created = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return InventoryMappings.ToDto(created!);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.UpdateDetails(
            request.Barcode,
            request.Name,
            request.Description,
            request.Category,
            request.CostPrice,
            request.SalePrice,
            request.ReorderLevel,
            request.ProviderId);

        await _productRepository.SaveChangesAsync(cancellationToken);

        var updated = await _productRepository.GetByIdAsync(id, cancellationToken);
        return InventoryMappings.ToDto(updated!);
    }

    public async Task<LocationStockDto?> UpdateLocationStockAsync(
        Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var stock = await _locationStockRepository.GetAsync(locationId, productId, cancellationToken);
        if (stock is null)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null) return null;

            stock = new LocationStock(locationId, productId, 0, product.ReorderLevel);
            await _locationStockRepository.AddAsync(stock, cancellationToken);
        }

        stock.UpdateQuantity(quantity);

        // Recompute global aggregate as sum of all location stocks
        var allStocks = await _locationStockRepository.GetByProductAsync(productId, cancellationToken);
        var totalAcrossLocations = allStocks.Sum(s => s.Quantity);
        var p = await _productRepository.GetByIdAsync(productId, cancellationToken);
        p?.SetStockQuantity(totalAcrossLocations);

        await _locationStockRepository.SaveChangesAsync(cancellationToken);
        return InventoryMappings.ToDto(stock);
    }

    public async Task<IReadOnlyList<LocationStockDto>> GetStockByLocationAsync(
        Guid locationId, CancellationToken cancellationToken)
    {
        var stocks = await _locationStockRepository.GetByLocationAsync(locationId, cancellationToken);
        return stocks.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<int> GetLocationStockQuantityAsync(
        Guid locationId, Guid productId, CancellationToken cancellationToken)
    {
        var stock = await _locationStockRepository.GetAsync(locationId, productId, cancellationToken);
        return stock?.Quantity ?? 0;
    }
}
