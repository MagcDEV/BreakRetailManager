using BreakRetailManager.Sales.Contracts;
using Microsoft.JSInterop;

namespace BreakRetailManager.Client.Services;

public sealed class IndexedDbSalesStore
{
    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public IndexedDbSalesStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _jsRuntime.InvokeVoidAsync("breakRetailDb.init");
        _initialized = true;
    }

    public async Task<IReadOnlyList<SalesOrderDto>> GetCachedOrdersAsync()
    {
        await InitializeAsync();
        return await _jsRuntime.InvokeAsync<List<SalesOrderDto>>("breakRetailDb.getOrders");
    }

    public async Task SetCachedOrdersAsync(IEnumerable<SalesOrderDto> orders)
    {
        await InitializeAsync();
        await _jsRuntime.InvokeVoidAsync("breakRetailDb.setOrders", orders);
    }

    public async Task<IReadOnlyList<OutboxOrder>> GetOutboxAsync()
    {
        await InitializeAsync();
        return await _jsRuntime.InvokeAsync<List<OutboxOrder>>("breakRetailDb.getOutbox");
    }

    public async Task AddOutboxAsync(OutboxOrder order)
    {
        await InitializeAsync();
        await _jsRuntime.InvokeVoidAsync("breakRetailDb.addOutbox", order);
    }

    public async Task RemoveOutboxAsync(Guid id)
    {
        await InitializeAsync();
        await _jsRuntime.InvokeVoidAsync("breakRetailDb.removeOutbox", id);
    }
}
