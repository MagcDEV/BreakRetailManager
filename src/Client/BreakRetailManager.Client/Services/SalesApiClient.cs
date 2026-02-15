using System.Net.Http.Json;
using BreakRetailManager.Sales.Contracts;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Client.Services;

public sealed class SalesApiClient
{
    private const string OrdersEndpoint = "api/sales/orders";
    private const string OffersEndpoint = "api/sales/offers";
    private const string ActiveOffersEndpoint = "api/sales/offers/active";

    private readonly HttpClient _httpClient;
    private readonly IndexedDbSalesStore _store;
    private readonly ConnectivityService _connectivity;
    private readonly ILogger<SalesApiClient> _logger;

    public SalesApiClient(
        HttpClient httpClient,
        IndexedDbSalesStore store,
        ConnectivityService connectivity,
        ILogger<SalesApiClient> logger)
    {
        _httpClient = httpClient;
        _store = store;
        _connectivity = connectivity;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SalesOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        if (!await _connectivity.IsOnlineAsync())
        {
            _logger.LogInformation("Offline detected, returning cached sales orders.");
            return await _store.GetCachedOrdersAsync();
        }

        try
        {
            var orders = await _httpClient.GetFromJsonAsync<List<SalesOrderDto>>(OrdersEndpoint, cancellationToken);
            if (orders is null)
            {
                return Array.Empty<SalesOrderDto>();
            }

            await _store.SetCachedOrdersAsync(orders);
            return orders;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to reach API, returning cached sales orders.");
            return await _store.GetCachedOrdersAsync();
        }
    }

    public async Task<SalesOrderDto> CreateOrderAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _connectivity.IsOnlineAsync())
        {
            return await EnqueueOfflineOrderAsync(request);
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync(OrdersEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<SalesOrderDto>(cancellationToken: cancellationToken);
            if (created is null)
            {
                throw new InvalidOperationException("The API did not return a created order.");
            }

            var cached = await _store.GetCachedOrdersAsync();
            var updated = cached.ToList();
            updated.Insert(0, created);
            await _store.SetCachedOrdersAsync(updated);

            return created;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to reach API, storing order offline.");
            return await EnqueueOfflineOrderAsync(request);
        }
    }

    public async Task<IReadOnlyList<OfferDto>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<OfferDto>>(OffersEndpoint, cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to fetch offers.");
            return [];
        }
    }

    public async Task<IReadOnlyList<OfferDto>> GetActiveOffersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<OfferDto>>(ActiveOffersEndpoint, cancellationToken)
                ?? [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to fetch active offers.");
            return [];
        }
    }

    public async Task<OfferDto?> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(OffersEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OfferDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to create offer.");
            return null;
        }
    }

    public async Task<OfferDto?> UpdateOfferAsync(Guid id, UpdateOfferRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{OffersEndpoint}/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OfferDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to update offer {OfferId}.", id);
            return null;
        }
    }

    public async Task<OfferDto?> ActivateOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{OffersEndpoint}/{id}/activate", content: null, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OfferDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to activate offer {OfferId}.", id);
            return null;
        }
    }

    public async Task<OfferDto?> DeactivateOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{OffersEndpoint}/{id}/deactivate", content: null, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OfferDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to deactivate offer {OfferId}.", id);
            return null;
        }
    }

    public async Task<bool> DeleteOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{OffersEndpoint}/{id}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to delete offer {OfferId}.", id);
            return false;
        }
    }

    public async Task<int> SyncOutboxAsync(CancellationToken cancellationToken = default)
    {
        if (!await _connectivity.IsOnlineAsync())
        {
            return 0;
        }

        var outbox = await _store.GetOutboxAsync();
        var synced = 0;

        foreach (var item in outbox.OrderBy(order => order.CreatedAt))
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(OrdersEndpoint, item.Request, cancellationToken);
                response.EnsureSuccessStatusCode();

                await _store.RemoveOutboxAsync(item.Id);
                synced++;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Failed to sync offline order {OrderId}.", item.Id);
                break;
            }
        }

        if (synced > 0)
        {
            await RefreshCacheAsync(cancellationToken);
        }

        return synced;
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _httpClient.GetFromJsonAsync<List<SalesOrderDto>>(OrdersEndpoint, cancellationToken);
            if (orders is not null)
            {
                await _store.SetCachedOrdersAsync(orders);
            }
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to refresh cached orders after sync.");
        }
    }

    private async Task<SalesOrderDto> EnqueueOfflineOrderAsync(CreateSalesOrderRequest request)
    {
        var localId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var lines = request.Lines
            .Select(line => new SalesOrderLineDto(Guid.NewGuid(), line.ProductId, line.ProductName, line.Quantity, line.UnitPrice))
            .ToList();

        var order = new SalesOrderDto(
            localId,
            $"OFFLINE-{createdAt:yyyyMMdd-HHmmss}",
            createdAt,
            lines.Sum(line => line.UnitPrice * line.Quantity),
            lines,
            PaymentMethod: request.PaymentMethod,
            LocationId: request.LocationId,
            Cae: null,
            CaeExpirationDate: null,
            InvoiceNumber: 0,
            PointOfSale: 0,
            Subtotal: lines.Sum(line => line.UnitPrice * line.Quantity),
            DiscountTotal: 0);

        await _store.AddOutboxAsync(new OutboxOrder(localId, request, createdAt));

        var cached = await _store.GetCachedOrdersAsync();
        var updated = cached.ToList();
        updated.Insert(0, order);
        await _store.SetCachedOrdersAsync(updated);

        return order;
    }
}
