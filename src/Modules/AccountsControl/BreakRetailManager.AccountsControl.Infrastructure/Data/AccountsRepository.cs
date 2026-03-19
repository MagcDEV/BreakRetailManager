using BreakRetailManager.AccountsControl.Application;
using BreakRetailManager.AccountsControl.Domain;
using BreakRetailManager.AccountsControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.AccountsControl.Infrastructure.Data;

public sealed class AccountsRepository : IAccountsRepository
{
    private readonly AccountsControlDbContext _dbContext;

    public AccountsRepository(AccountsControlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Account>> GetAccountsWithMovementsAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Accounts
            .Include(account => account.Movements)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(account => account.IsActive);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetActiveAccountsByTypeWithMovementsAsync(
        AccountType type,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .Include(account => account.Movements)
            .Where(account => account.Type == type && account.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task<Account?> GetAccountWithMovementsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Accounts
            .Include(account => account.Movements)
            .FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Movement> Items, int TotalCount)> GetMovementsPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.Movements
            .Include(movement => movement.Account)
            .OrderByDescending(movement => movement.CreatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> AccountNameExistsAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return _dbContext.Accounts.AnyAsync(account => account.NormalizedName == normalizedName, cancellationToken);
    }

    public Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        return _dbContext.Accounts.AddAsync(account, cancellationToken).AsTask();
    }

    public Task AddMovementAsync(Account account, Movement movement, CancellationToken cancellationToken = default)
    {
        return _dbContext.Movements.AddAsync(movement, cancellationToken).AsTask();
    }

    public Task AddAuditLogAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        return _dbContext.AuditLogs.AddAsync(entry, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
