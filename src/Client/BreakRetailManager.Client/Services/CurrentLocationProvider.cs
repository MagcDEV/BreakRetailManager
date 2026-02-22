using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BreakRetailManager.Client.Services;

/// <summary>
/// Stores the currently selected store location in browser storage so it persists
/// across navigation and refresh, and provides a change event for UI components.
/// </summary>
public sealed class CurrentLocationProvider : IDisposable
{
    private const string StorageKey = "brm.currentLocation";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<CurrentLocationProvider> _logger;
    private bool _initialized;
    private bool _wasAuthenticated;

    public Guid LocationId { get; private set; }

    public string LocationName { get; private set; } = string.Empty;

    public bool HasLocation => LocationId != Guid.Empty;

    public event Action? LocationChanged;

    public CurrentLocationProvider(
        AuthenticationStateProvider authStateProvider,
        IJSRuntime jsRuntime,
        ILogger<CurrentLocationProvider> logger)
    {
        _authStateProvider = authStateProvider;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _authStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        _wasAuthenticated = authState.User.Identity?.IsAuthenticated == true;

        await LoadFromStorageAsync();
    }

    public async Task SetLocationAsync(Guid locationId, string locationName)
    {
        if (locationId == Guid.Empty || string.IsNullOrWhiteSpace(locationName))
        {
            await ClearAsync();
            return;
        }

        LocationId = locationId;
        LocationName = locationName;

        try
        {
            var json = JsonSerializer.Serialize(new StoredLocation(locationId, locationName), JsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to persist current location selection to localStorage.");
        }

        LocationChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        LocationId = Guid.Empty;
        LocationName = string.Empty;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to clear current location selection from localStorage.");
        }

        LocationChanged?.Invoke();
    }

    private async Task LoadFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var stored = JsonSerializer.Deserialize<StoredLocation>(json, JsonOptions);
            if (stored is null || stored.LocationId == Guid.Empty || string.IsNullOrWhiteSpace(stored.LocationName))
            {
                return;
            }

            LocationId = stored.LocationId;
            LocationName = stored.LocationName;
            LocationChanged?.Invoke();
        }
        catch (Exception ex) when (ex is JSException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to load current location selection from localStorage.");
        }
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var authState = await task;
        var isAuthenticated = authState.User.Identity?.IsAuthenticated == true;

        // Only clear on an auth transition to signed-out; don't wipe persisted selection
        // on initial app load when the user simply hasn't signed in yet.
        if (!isAuthenticated && _wasAuthenticated && HasLocation)
        {
            await ClearAsync();
        }

        _wasAuthenticated = isAuthenticated;
    }

    public void Dispose()
    {
        _authStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
    }

    private sealed record StoredLocation(Guid LocationId, string LocationName);
}

