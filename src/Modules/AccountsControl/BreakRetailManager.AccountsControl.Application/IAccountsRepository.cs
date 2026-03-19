using BreakRetailManager.AccountsControl.Domain;
using BreakRetailManager.AccountsControl.Domain.Entities;

namespace BreakRetailManager.AccountsControl.Application;

public interface IAccountsRepository
{
    Task<IReadOnlyList<Account>> GetAccountsWithMovementsAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetActiveAccountsByTypeWithMovementsAsync(
        AccountType type,
        CancellationToken cancellationToken = default);

    Task<Account?> GetAccountWithMovementsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Movement> Items, int TotalCount)> GetMovementsPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> AccountNameExistsAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task AddAccountAsync(Account account, CancellationToken cancellationToken = default);

    Task AddMovementAsync(Account account, Movement movement, CancellationToken cancellationToken = default);

    Task AddAuditLogAsync(AuditLog entry, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
