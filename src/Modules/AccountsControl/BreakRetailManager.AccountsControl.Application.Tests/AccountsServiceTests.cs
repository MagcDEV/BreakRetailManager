using BreakRetailManager.AccountsControl.Contracts;
using BreakRetailManager.AccountsControl.Domain;
using BreakRetailManager.AccountsControl.Domain.Entities;

namespace BreakRetailManager.AccountsControl.Application.Tests;

public sealed class AccountsServiceTests
{
    [Fact]
    public async Task CreateEmployeeMovementAsync_AddsPositiveChargeAndUpdatesBalance()
    {
        var employee = new Account("Alice", Domain.AccountType.Employee, DateTimeOffset.UtcNow.AddDays(-1));
        var repository = new InMemoryAccountsRepository([employee]);
        var service = new AccountsService(repository);

        var created = await service.CreateEmployeeMovementAsync(
            employee.Id,
            new CreateMovementRequest("Snacks", 150m, "Morning"));

        Assert.NotNull(created);
        Assert.Equal(150m, created.Amount);
        Assert.Equal(1, employee.MovementCount);
        Assert.Equal(150m, employee.Balance);
    }

    [Fact]
    public async Task CreateExpenseMovementAsync_RejectsNegativeAmount()
    {
        var expense = new Account("Cleaning", Domain.AccountType.GeneralExpense, DateTimeOffset.UtcNow.AddDays(-1));
        var repository = new InMemoryAccountsRepository([expense]);
        var service = new AccountsService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateExpenseMovementAsync(
                expense.Id,
                new CreateMovementRequest("Supplies", -10m)));
    }

    [Fact]
    public async Task CreateAccountAsync_RejectsDuplicateNamesCaseInsensitively()
    {
        var existing = new Account("Alice", Domain.AccountType.Employee, DateTimeOffset.UtcNow.AddDays(-1));
        var repository = new InMemoryAccountsRepository([existing]);
        var service = new AccountsService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(
                new CreateAccountRequest("alice", Contracts.AccountType.Employee),
                "admin-oid"));
    }

    [Fact]
    public async Task CreateAdminAdjustmentAsync_CreatesNegativeMovementForDecrease()
    {
        var employee = new Account("Bob", Domain.AccountType.Employee, DateTimeOffset.UtcNow.AddDays(-1));
        employee.AddMovement(Movement.CreateEmployeeCharge(employee.Id, "Lunch", 100m, "Night", DateTimeOffset.UtcNow.AddHours(-2)));

        var repository = new InMemoryAccountsRepository([employee]);
        var service = new AccountsService(repository);

        var created = await service.CreateAdminAdjustmentAsync(
            employee.Id,
            new CreateAdminAdjustmentRequest("Payment", 40m, AdminAdjustmentDirection.Decrease),
            "admin-oid");

        Assert.NotNull(created);
        Assert.Equal(-40m, created.Amount);
        Assert.Equal(60m, employee.Balance);
        Assert.Single(repository.AuditLogs.Where(log => log.Action == "AdminAdjustmentCreated"));
    }

    private sealed class InMemoryAccountsRepository : IAccountsRepository
    {
        private readonly List<Account> _accounts;
        private readonly List<AuditLog> _auditLogs = [];

        public InMemoryAccountsRepository(IEnumerable<Account> accounts)
        {
            _accounts = accounts.ToList();
        }

        public IReadOnlyList<AuditLog> AuditLogs => _auditLogs;

        public Task<IReadOnlyList<Account>> GetAccountsWithMovementsAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
        {
            var items = includeInactive
                ? _accounts.ToList()
                : _accounts.Where(account => account.IsActive).ToList();

            return Task.FromResult<IReadOnlyList<Account>>(items);
        }

        public Task<IReadOnlyList<Account>> GetActiveAccountsByTypeWithMovementsAsync(Domain.AccountType type, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Account>>(
                _accounts.Where(account => account.IsActive && account.Type == type).ToList());
        }

        public Task<Account?> GetAccountWithMovementsAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accounts.FirstOrDefault(account => account.Id == accountId));
        }

        public Task<(IReadOnlyList<Movement> Items, int TotalCount)> GetMovementsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var items = _accounts
                .SelectMany(account => account.Movements)
                .OrderByDescending(movement => movement.CreatedAt)
                .ToList();

            return Task.FromResult<(IReadOnlyList<Movement> Items, int TotalCount)>(
                (items.Skip((page - 1) * pageSize).Take(pageSize).ToList(), items.Count));
        }

        public Task<bool> AccountNameExistsAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accounts.Any(account => account.NormalizedName == normalizedName));
        }

        public Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task AddMovementAsync(Account account, Movement movement, CancellationToken cancellationToken = default)
        {
            account.AddMovement(movement);
            return Task.CompletedTask;
        }

        public Task AddAuditLogAsync(AuditLog entry, CancellationToken cancellationToken = default)
        {
            _auditLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
