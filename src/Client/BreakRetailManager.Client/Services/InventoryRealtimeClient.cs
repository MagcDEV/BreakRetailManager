using BreakRetailManager.Inventory.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Client.Services;

public sealed class InventoryRealtimeClient : IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ILogger<InventoryRealtimeClient> _logger;

    private HubConnection? _connection;

    public InventoryRealtimeClient(
        NavigationManager navigationManager,
        IAccessTokenProvider tokenProvider,
        ILogger<InventoryRealtimeClient> logger)
    {
        _navigationManager = navigationManager;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public event Func<InventoryStockChangedEvent, Task>? StockChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        var hubUrl = _navigationManager.ToAbsoluteUri("/hubs/inventory");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var result = await _tokenProvider.RequestAccessToken();
                    return result.TryGetToken(out var token) ? token.Value : null;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<InventoryStockChangedEvent>("InventoryStockChanged", async message =>
        {
            var handler = StockChanged;
            if (handler is null)
            {
                return;
            }

            await handler.Invoke(message);
        });

        try
        {
            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to inventory realtime hub at {HubUrl}.", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to inventory realtime hub.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.DisposeAsync();
        _connection = null;
    }
}