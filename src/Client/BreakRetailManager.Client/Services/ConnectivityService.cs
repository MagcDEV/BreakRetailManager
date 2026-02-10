using Microsoft.JSInterop;

namespace BreakRetailManager.Client.Services;

public sealed class ConnectivityService
{
    private readonly IJSRuntime _jsRuntime;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask<bool> IsOnlineAsync()
    {
        return _jsRuntime.InvokeAsync<bool>("breakRetailDb.isOnline");
    }
}
