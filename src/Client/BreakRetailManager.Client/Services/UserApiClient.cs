using System.Net.Http.Json;
using BreakRetailManager.UserManagement.Contracts;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Client.Services;

public sealed class UserApiClient
{
    private const string UsersEndpoint = "api/users";

    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiClient> _logger;

    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>($"{UsersEndpoint}/me", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get current user.");
            return null;
        }
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserDto>>(UsersEndpoint, cancellationToken)
                   ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get users.");
            return [];
        }
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoleDto>>($"{UsersEndpoint}/roles", cancellationToken)
                   ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get roles.");
            return [];
        }
    }

    public async Task<UserDto?> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{UsersEndpoint}/{userId}/roles",
                new AssignRoleRequest(roleName),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to assign role {Role} to user {UserId}.", roleName, userId);
            return null;
        }
    }

    public async Task<UserDto?> RevokeRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"{UsersEndpoint}/{userId}/roles/{roleName}",
                cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to revoke role {Role} from user {UserId}.", roleName, userId);
            return null;
        }
    }
}
