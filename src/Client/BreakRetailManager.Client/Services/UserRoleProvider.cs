using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BreakRetailManager.Client.Services;

/// <summary>
/// Automatically calls /api/users/me when the user signs in,
/// provisioning them in the database and caching their roles locally.
/// </summary>
public sealed class UserRoleProvider : IDisposable
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserApiClient _userApi;
    private bool _provisioned;

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public event Action? RolesChanged;

    public UserRoleProvider(AuthenticationStateProvider authStateProvider, UserApiClient userApi)
    {
        _authStateProvider = authStateProvider;
        _userApi = userApi;
        _authStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
    }

    public async Task InitializeAsync()
    {
        if (_provisioned) return;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        await ProvisionIfAuthenticatedAsync(authState);
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var authState = await task;
        await ProvisionIfAuthenticatedAsync(authState);
    }

    private async Task ProvisionIfAuthenticatedAsync(AuthenticationState authState)
    {
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            _provisioned = false;
            Roles = [];
            return;
        }

        if (_provisioned) return;

        var user = await _userApi.GetCurrentUserAsync();
        if (user is not null)
        {
            Roles = user.Roles;
            _provisioned = true;

            // Add role claims to the current identity so AuthorizeView Roles="Admin" works
            var identity = authState.User.Identity as ClaimsIdentity;
            if (identity is not null)
            {
                foreach (var role in user.Roles)
                {
                    if (!identity.HasClaim(ClaimTypes.Role, role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }

            RolesChanged?.Invoke();
        }
    }

    public void Dispose()
    {
        _authStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
