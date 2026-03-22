using BreakRetailManager.BuildingBlocks.Pagination;
using BreakRetailManager.Inventory.Contracts;
using BreakRetailManager.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BreakRetailManager.Inventory.Application;

public sealed class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILocationStockRepository _locationStockRepository;
    private readonly IMemoryCache _cache;

    public ProductService(
        IProductRepository productRepository,
        ILocationStockRepository locationStockRepository,
        IMemoryCache cache)
    {
        _productRepository = productRepository;
        _locationStockRepository = locationStockRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return await EnrichWithStockTotals(products, cancellationToken);
    }

    public async Task<PagedResult<ProductDto>> GetProductsPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var items = await EnrichWithStockTotals(products, cancellationToken);
        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var dto = InventoryMappings.ToDto(product);
        var stockTotals = await _locationStockRepository.GetTotalsByProductAsync([id], cancellationToken);

        return stockTotals.TryGetValue(id, out var total)
            ? dto with { StockQuantity = total, IsLowStock = total <= dto.ReorderLevel }
            : dto;
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var cacheKey = $"product-barcode:{barcode}";

        if (_cache.TryGetValue(cacheKey, out ProductDto? cached))
        {
            return cached;
        }

        var product = await _productRepository.GetByBarcodeAsync(barcode, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var dto = InventoryMappings.ToDto(product);
        var stockTotals = await _locationStockRepository.GetTotalsByProductAsync([product.Id], cancellationToken);

        var result = stockTotals.TryGetValue(product.Id, out var total)
            ? dto with { StockQuantity = total, IsLowStock = total <= dto.ReorderLevel }
            : dto;

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
        return result;
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken)
    {
        var lowStockItems = await _productRepository.GetLowStockWithTotalsAsync(cancellationToken);
        return lowStockItems
            .Select(item =>
            {
                var dto = InventoryMappings.ToDto(item.Product);
                return dto with { StockQuantity = item.StockTotal, IsLowStock = true };
            })
            .ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = InventoryMappings.ToProduct(request);
        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        _cache.Remove($"product-barcode:{product.Barcode}");
        return InventoryMappings.ToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var oldBarcode = product.Barcode;
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

        _cache.Remove($"product-barcode:{oldBarcode}");
        if (oldBarcode != request.Barcode)
        {
            _cache.Remove($"product-barcode:{request.Barcode}");
        }

        return InventoryMappings.ToDto(product);
    }

    public async Task<LocationStockDto?> UpdateLocationStockAsync(
        Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await TryUpdateLocationStockAsync(locationId, productId, quantity, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                // Retry with fresh data on next iteration
            }
        }

        // Final attempt — let the exception propagate
        return await TryUpdateLocationStockAsync(locationId, productId, quantity, cancellationToken);
    }

    private async Task<LocationStockDto?> TryUpdateLocationStockAsync(
        Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var stock = await _locationStockRepository.GetAsync(locationId, productId, cancellationToken);
        var stockExisted = stock is not null;

        if (!stockExisted)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null) return null;

            stock = new LocationStock(locationId, productId, 0, product.ReorderLevel);
            await _locationStockRepository.AddAsync(stock, cancellationToken);
        }

        if (stock is null)
        {
            return null;
        }

        stock.UpdateQuantity(quantity);

        await _locationStockRepository.SaveChangesAsync(cancellationToken);
        return InventoryMappings.ToDto(stock);
    }

    public async Task<IReadOnlyList<LocationStockDto>> GetStockByLocationAsync(
        Guid locationId, CancellationToken cancellationToken)
    {
        var stocks = await _locationStockRepository.GetByLocationAsync(locationId, cancellationToken);
        return stocks.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<IReadOnlyList<LocationStockDto>> GetStockByProductAsync(
        Guid productId, CancellationToken cancellationToken)
    {
        var stocks = await _locationStockRepository.GetByProductAsync(productId, cancellationToken);
        return stocks.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<int> GetLocationStockQuantityAsync(
        Guid locationId, Guid productId, CancellationToken cancellationToken)
    {
        var stock = await _locationStockRepository.GetAsync(locationId, productId, cancellationToken);
        return stock?.Quantity ?? 0;
    }

    private async Task<IReadOnlyList<ProductDto>> EnrichWithStockTotals(
        IReadOnlyList<Product> products, CancellationToken cancellationToken)
    {
        var stockTotals = await _locationStockRepository.GetTotalsByProductAsync(
            products.Select(product => product.Id).ToArray(),
            cancellationToken);

        return products
            .Select(product =>
            {
                var dto = InventoryMappings.ToDto(product);
                return stockTotals.TryGetValue(product.Id, out var total)
                    ? dto with { StockQuantity = total, IsLowStock = total <= dto.ReorderLevel }
                    : dto;
            })
            .ToList();
    }
}
