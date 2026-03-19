using System.Net;
using System.Net.Http.Json;
using BreakRetailManager.AccountsControl.Contracts;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Client.Services;

public sealed class AccountsControlApiClient
{
    private const string AccountsEndpoint = "api/accounts";

    private readonly HttpClient _publicHttpClient;
    private readonly HttpClient _authorizedHttpClient;
    private readonly ILogger<AccountsControlApiClient> _logger;

    public AccountsControlApiClient(IHttpClientFactory httpClientFactory, ILogger<AccountsControlApiClient> logger)
    {
        _publicHttpClient = httpClientFactory.CreateClient("PublicApiClient");
        _authorizedHttpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
    }

    public event Action? StoreBalanceChanged;

    public async Task<PublicSummaryDto?> GetPublicSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _publicHttpClient.GetFromJsonAsync<PublicSummaryDto>($"{AccountsEndpoint}/summary", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load accounts summary.");
            return null;
        }
    }

    public async Task<IReadOnlyList<AccountOptionDto>> GetEmployeeAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _publicHttpClient.GetFromJsonAsync<List<AccountOptionDto>>($"{AccountsEndpoint}/employees", cancellationToken) ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load employee accounts.");
            return [];
        }
    }

    public async Task<IReadOnlyList<AccountOptionDto>> GetExpenseAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _publicHttpClient.GetFromJsonAsync<List<AccountOptionDto>>($"{AccountsEndpoint}/expenses", cancellationToken) ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load expense accounts.");
            return [];
        }
    }

    public async Task<AccountSummaryDto?> GetEmployeeAccountSummaryAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<AccountSummaryDto>($"{AccountsEndpoint}/employees/{accountId}", cancellationToken);
    }

    public async Task<IReadOnlyList<MovementDto>> GetEmployeeMovementsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await GetPublicListAsync<MovementDto>($"{AccountsEndpoint}/employees/{accountId}/movements", cancellationToken);
    }

    public async Task<MovementDto?> CreateEmployeeMovementAsync(Guid accountId, CreateMovementRequest request, CancellationToken cancellationToken = default)
    {
        return await PostPublicAsync<MovementDto>($"{AccountsEndpoint}/employees/{accountId}/movements", request, raiseBalanceChanged: true, cancellationToken);
    }

    public async Task<AccountSummaryDto?> GetExpenseAccountSummaryAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await GetPublicAsync<AccountSummaryDto>($"{AccountsEndpoint}/expenses/{accountId}", cancellationToken);
    }

    public async Task<IReadOnlyList<MovementDto>> GetExpenseMovementsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await GetPublicListAsync<MovementDto>($"{AccountsEndpoint}/expenses/{accountId}/movements", cancellationToken);
    }

    public async Task<MovementDto?> CreateExpenseMovementAsync(Guid accountId, CreateMovementRequest request, CancellationToken cancellationToken = default)
    {
        return await PostPublicAsync<MovementDto>($"{AccountsEndpoint}/expenses/{accountId}/movements", request, raiseBalanceChanged: true, cancellationToken);
    }

    public async Task<AdminDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _authorizedHttpClient.GetFromJsonAsync<AdminDashboardDto>($"{AccountsEndpoint}/admin/dashboard", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load accounts dashboard.");
            return null;
        }
    }

    public async Task<AdminAccountDetailDto?> GetAdminAccountDetailAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _authorizedHttpClient.GetFromJsonAsync<AdminAccountDetailDto>($"{AccountsEndpoint}/admin/accounts/{accountId}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load account detail for {AccountId}.", accountId);
            return null;
        }
    }

    public async Task<AccountSummaryDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _authorizedHttpClient.PostAsJsonAsync($"{AccountsEndpoint}/admin/accounts", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<AccountSummaryDto>(cancellationToken: cancellationToken);
            if (created is not null)
            {
                StoreBalanceChanged?.Invoke();
            }

            return created;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to create account.");
            return null;
        }
    }

    public async Task<bool> DeactivateAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _authorizedHttpClient.DeleteAsync($"{AccountsEndpoint}/admin/accounts/{accountId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            StoreBalanceChanged?.Invoke();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to deactivate account {AccountId}.", accountId);
            return false;
        }
    }

    public async Task<MovementDto?> CreateAdminAdjustmentAsync(
        Guid accountId,
        CreateAdminAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _authorizedHttpClient.PostAsJsonAsync(
                $"{AccountsEndpoint}/admin/accounts/{accountId}/adjustments",
                request,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<MovementDto>(cancellationToken: cancellationToken);
            if (created is not null)
            {
                StoreBalanceChanged?.Invoke();
            }

            return created;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to create admin adjustment for account {AccountId}.", accountId);
            return null;
        }
    }

    public async Task<MovementPageDto> GetAdminMovementsAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _authorizedHttpClient.GetFromJsonAsync<MovementPageDto>(
                       $"{AccountsEndpoint}/admin/movements?page={page}&pageSize={pageSize}",
                       cancellationToken)
                   ?? new MovementPageDto([], 0, page, pageSize);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load admin movements.");
            return new MovementPageDto([], 0, page, pageSize);
        }
    }

    private async Task<T?> GetPublicAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await _publicHttpClient.GetFromJsonAsync<T>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load public accounts resource {Url}.", url);
            return default;
        }
    }

    private async Task<IReadOnlyList<T>> GetPublicListAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await _publicHttpClient.GetFromJsonAsync<List<T>>(url, cancellationToken) ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to load public accounts list {Url}.", url);
            return [];
        }
    }

    private async Task<T?> PostPublicAsync<T>(
        string url,
        object request,
        bool raiseBalanceChanged,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _publicHttpClient.PostAsJsonAsync(url, request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            if (created is not null && raiseBalanceChanged)
            {
                StoreBalanceChanged?.Invoke();
            }

            return created;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to post public accounts request {Url}.", url);
            return default;
        }
    }
}
