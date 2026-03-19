using BreakRetailManager.AccountsControl.Contracts;
using BreakRetailManager.AccountsControl.Domain.Entities;
using DomainAccountType = BreakRetailManager.AccountsControl.Domain.AccountType;

namespace BreakRetailManager.AccountsControl.Application;

public sealed class AccountsService
{
    private readonly IAccountsRepository _repository;

    public AccountsService(IAccountsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PublicSummaryDto> GetPublicSummaryAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetAccountsWithMovementsAsync(includeInactive: true, cancellationToken);
        var activeAccounts = accounts.Where(account => account.IsActive).ToList();

        return new PublicSummaryDto(
            decimal.Round(
                activeAccounts
                    .Where(account => account.Type == DomainAccountType.Employee)
                    .Sum(account => account.Balance),
                2,
                MidpointRounding.AwayFromZero),
            decimal.Round(
                activeAccounts
                    .Where(account => account.Type == DomainAccountType.GeneralExpense)
                    .Sum(account => account.Balance),
                2,
                MidpointRounding.AwayFromZero),
            decimal.Round(
                Math.Abs(activeAccounts
                    .SelectMany(account => account.Movements)
                    .Where(movement => movement.IsAdminAdjustment && movement.Amount < 0)
                    .Sum(movement => movement.Amount)),
                2,
                MidpointRounding.AwayFromZero));
    }

    public async Task<IReadOnlyList<AccountOptionDto>> GetActiveAccountsByTypeAsync(
        AccountType type,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetActiveAccountsByTypeWithMovementsAsync(
            AccountsMappings.ToDomainAccountType(type),
            cancellationToken);

        return accounts
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(AccountsMappings.ToOptionDto)
            .ToList();
    }

    public async Task<AccountSummaryDto?> GetEmployeeAccountSummaryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.Employee, cancellationToken);
        return account is null ? null : AccountsMappings.ToSummaryDto(account);
    }

    public async Task<IReadOnlyList<MovementDto>> GetEmployeeMovementsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.Employee, cancellationToken);
        return account is null
            ? []
            : account.Movements
                .OrderByDescending(movement => movement.CreatedAt)
                .Select(movement => AccountsMappings.ToMovementDto(movement, account))
                .ToList();
    }

    public async Task<MovementDto?> CreateEmployeeMovementAsync(
        Guid accountId,
        CreateMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.Employee, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var movement = Movement.CreateEmployeeCharge(
            account.Id,
            request.Description,
            request.Amount,
            request.Shift ?? string.Empty,
            DateTimeOffset.UtcNow);

        await _repository.AddMovementAsync(account, movement, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return AccountsMappings.ToMovementDto(movement, account);
    }

    public async Task<AccountSummaryDto?> GetExpenseAccountSummaryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.GeneralExpense, cancellationToken);
        return account is null ? null : AccountsMappings.ToSummaryDto(account);
    }

    public async Task<IReadOnlyList<MovementDto>> GetExpenseMovementsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.GeneralExpense, cancellationToken);
        return account is null
            ? []
            : account.Movements
                .OrderByDescending(movement => movement.CreatedAt)
                .Select(movement => AccountsMappings.ToMovementDto(movement, account))
                .ToList();
    }

    public async Task<MovementDto?> CreateExpenseMovementAsync(
        Guid accountId,
        CreateMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await GetActiveSelfServiceAccountAsync(accountId, DomainAccountType.GeneralExpense, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var movement = Movement.CreateExpense(
            account.Id,
            request.Description,
            request.Amount,
            DateTimeOffset.UtcNow);

        await _repository.AddMovementAsync(account, movement, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return AccountsMappings.ToMovementDto(movement, account);
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetAccountsWithMovementsAsync(includeInactive: true, cancellationToken);
        var activeAccounts = accounts.Where(account => account.IsActive).ToList();

        return new AdminDashboardDto(
            decimal.Round(
                activeAccounts
                    .Where(account => account.Type == DomainAccountType.Employee)
                    .Sum(account => account.Balance),
                2,
                MidpointRounding.AwayFromZero),
            decimal.Round(
                activeAccounts
                    .Where(account => account.Type == DomainAccountType.GeneralExpense)
                    .Sum(account => account.Balance),
                2,
                MidpointRounding.AwayFromZero),
            decimal.Round(
                Math.Abs(activeAccounts
                    .SelectMany(account => account.Movements)
                    .Where(movement => movement.IsAdminAdjustment && movement.Amount < 0)
                    .Sum(movement => movement.Amount)),
                2,
                MidpointRounding.AwayFromZero),
            accounts
                .Where(account => account.Type == DomainAccountType.Employee)
                .OrderByDescending(account => account.Balance)
                .ThenBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
                .Select(AccountsMappings.ToSummaryDto)
                .ToList(),
            accounts
                .Where(account => account.Type == DomainAccountType.GeneralExpense)
                .OrderByDescending(account => account.Balance)
                .ThenBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
                .Select(AccountsMappings.ToSummaryDto)
                .ToList());
    }

    public async Task<AdminAccountDetailDto?> GetAccountDetailAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountWithMovementsAsync(accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new AdminAccountDetailDto(
            AccountsMappings.ToSummaryDto(account),
            account.Movements
                .OrderByDescending(movement => movement.CreatedAt)
                .Select(movement => AccountsMappings.ToMovementDto(movement, account))
                .ToList());
    }

    public async Task<MovementDto?> CreateAdminAdjustmentAsync(
        Guid accountId,
        CreateAdminAdjustmentRequest request,
        string? adminObjectId,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountWithMovementsAsync(accountId, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return null;
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Adjustment amount must be greater than zero.");
        }

        var signedAmount = request.Direction == AdminAdjustmentDirection.Decrease
            ? -request.Amount
            : request.Amount;

        var createdAt = DateTimeOffset.UtcNow;
        var movement = Movement.CreateAdminAdjustment(
            account.Id,
            request.Description,
            signedAmount,
            createdAt,
            adminObjectId);

        await _repository.AddMovementAsync(account, movement, cancellationToken);
        await _repository.AddAuditLogAsync(
            new AuditLog(
                "AdminAdjustmentCreated",
                nameof(Account),
                account.Id.ToString(),
                adminObjectId ?? "unknown",
                createdAt,
                AccountsMappings.ToAuditPayload(new
                {
                    accountId = account.Id,
                    amount = movement.Amount,
                    direction = request.Direction.ToString()
                })),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return AccountsMappings.ToMovementDto(movement, account);
    }

    public async Task<MovementPageDto> GetAdminMovementsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.GetMovementsPagedAsync(safePage, safePageSize, cancellationToken);

        return new MovementPageDto(
            items.Select(AccountsMappings.ToMovementDto).ToList(),
            totalCount,
            safePage,
            safePageSize);
    }

    public async Task<AccountSummaryDto> CreateAccountAsync(
        CreateAccountRequest request,
        string? adminObjectId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = Account.NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Account name is required.", nameof(request));
        }

        if (await _repository.AccountNameExistsAsync(normalizedName, cancellationToken))
        {
            throw new InvalidOperationException($"An account named '{request.Name.Trim()}' already exists.");
        }

        var createdAt = DateTimeOffset.UtcNow;
        var account = new Account(
            request.Name,
            AccountsMappings.ToDomainAccountType(request.Type),
            createdAt);

        await _repository.AddAccountAsync(account, cancellationToken);
        await _repository.AddAuditLogAsync(
            new AuditLog(
                "AccountCreated",
                nameof(Account),
                account.Id.ToString(),
                adminObjectId ?? "unknown",
                createdAt,
                AccountsMappings.ToAuditPayload(new
                {
                    account.Id,
                    account.Name,
                    Type = request.Type.ToString()
                })),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return AccountsMappings.ToSummaryDto(account);
    }

    public async Task<bool> DeactivateAccountAsync(
        Guid accountId,
        string? adminObjectId,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountWithMovementsAsync(accountId, cancellationToken);
        if (account is null)
        {
            return false;
        }

        var deactivatedAt = DateTimeOffset.UtcNow;
        account.Deactivate(deactivatedAt);
        await _repository.AddAuditLogAsync(
            new AuditLog(
                "AccountDeactivated",
                nameof(Account),
                account.Id.ToString(),
                adminObjectId ?? "unknown",
                deactivatedAt,
                AccountsMappings.ToAuditPayload(new
                {
                    account.Id,
                    account.Name,
                    account.IsActive
                })),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Account?> GetActiveSelfServiceAccountAsync(
        Guid accountId,
        DomainAccountType expectedType,
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountWithMovementsAsync(accountId, cancellationToken);
        return account is not null && account.IsActive && account.Type == expectedType
            ? account
            : null;
    }
}
