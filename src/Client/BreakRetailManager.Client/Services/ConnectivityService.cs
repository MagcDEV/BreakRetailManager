using Microsoft.JSInterop;

namespace BreakRetailManager.Client.Services;

public sealed class ConnectivityService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    private bool _initialized;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsOnline { get; private set; } = true;

    public event Action? ConnectivityChanged;

    public async ValueTask<bool> IsOnlineAsync()
    {
        if (!_initialized)
        {
            IsOnline = await _jsRuntime.InvokeAsync<bool>("breakRetailDb.isOnline");
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("breakRetailConnectivity.register", _dotNetRef);
            _initialized = true;
        }

        return IsOnline;
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        IsOnline = isOnline;
        ConnectivityChanged?.Invoke();
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
