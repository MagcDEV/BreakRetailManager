using System.Net.Http.Json;
using BreakRetailManager.Inventory.Contracts;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Client.Services;

public sealed class InventoryApiClient
{
    private const string ProductsEndpoint = "api/inventory/products";
    private const string ProvidersEndpoint = "api/inventory/providers";
    private const string LocationsEndpoint = "api/inventory/locations";

    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryApiClient> _logger;

    public InventoryApiClient(HttpClient httpClient, ILogger<InventoryApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProductDto>>(ProductsEndpoint, cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to fetch products.");
            return [];
        }
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProductDto>>($"{ProductsEndpoint}/low-stock", cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to fetch low stock products.");
            return [];
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ProductsEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to create product.");
            return null;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ProductsEndpoint}/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to update product {ProductId}.", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProviderDto>>(ProvidersEndpoint, cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to fetch providers.");
            return [];
        }
    }

    public async Task<ProviderDto?> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ProvidersEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProviderDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to create provider.");
            return null;
        }
    }

    public async Task<ProviderDto?> UpdateProviderAsync(Guid id, UpdateProviderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ProvidersEndpoint}/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProviderDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to update provider {ProviderId}.", id);
            return null;
        }
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"{ProductsEndpoint}/barcode/{Uri.EscapeDataString(barcode)}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<LocationDto>>(LocationsEndpoint, cancellationToken)
                   ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch locations.");
            return [];
        }
    }

    public async Task<IReadOnlyList<LocationStockDto>> GetLocationStockAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<LocationStockDto>>($"{LocationsEndpoint}/{locationId}/stock", cancellationToken)
                   ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch stock for location {LocationId}.", locationId);
            return [];
        }
    }

    public async Task<LocationStockDto?> UpdateLocationStockAsync(Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"{LocationsEndpoint}/{locationId}/stock/{productId}", new { Quantity = quantity }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocationStockDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to update location stock for {LocationId}/{ProductId}.", locationId, productId);
            return null;
        }
    }

    public async Task<LocationStockDto?> UpdateLocationStockForSaleAsync(Guid locationId, Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity >= 0)
        {
            _logger.LogWarning("Sale stock adjustment must be negative; got {Quantity} for {LocationId}/{ProductId}.", quantity, locationId, productId);
            return null;
        }

        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"{LocationsEndpoint}/{locationId}/stock/{productId}/sale", new { Quantity = quantity }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocationStockDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to update location stock for sale {LocationId}/{ProductId}.", locationId, productId);
            return null;
        }
    }
}
