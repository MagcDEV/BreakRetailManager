namespace BreakRetailManager.AccountsControl.Domain.Entities;

public sealed class Movement
{
    private Movement()
    {
    }

    private Movement(
        Guid accountId,
        string description,
        decimal amount,
        string? shift,
        MovementType movementType,
        bool isAdminAdjustment,
        DateTimeOffset createdAt,
        MovementOriginRole createdByRole,
        string? createdByUserId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (amount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be zero.");
        }

        Id = Guid.NewGuid();
        AccountId = accountId;
        Description = description.Trim();
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Shift = string.IsNullOrWhiteSpace(shift) ? null : shift.Trim();
        MovementType = movementType;
        IsAdminAdjustment = isAdminAdjustment;
        CreatedAt = createdAt;
        CreatedByRole = createdByRole;
        CreatedByUserId = string.IsNullOrWhiteSpace(createdByUserId) ? null : createdByUserId.Trim();
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string? Shift { get; private set; }

    public MovementType MovementType { get; private set; }

    public bool IsAdminAdjustment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public MovementOriginRole CreatedByRole { get; private set; }

    public string? CreatedByUserId { get; private set; }

    public Account? Account { get; private set; }

    public static Movement CreateEmployeeCharge(
        Guid accountId,
        string description,
        decimal amount,
        string shift,
        DateTimeOffset createdAt)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Employee charges must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(shift))
        {
            throw new ArgumentException("Shift is required.", nameof(shift));
        }

        return new Movement(
            accountId,
            description,
            amount,
            shift,
            MovementType.Charge,
            isAdminAdjustment: false,
            createdAt,
            MovementOriginRole.Employee,
            createdByUserId: null);
    }

    public static Movement CreateExpense(
        Guid accountId,
        string description,
        decimal amount,
        DateTimeOffset createdAt)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense entries must be greater than zero.");
        }

        return new Movement(
            accountId,
            description,
            amount,
            shift: null,
            MovementType.Expense,
            isAdminAdjustment: false,
            createdAt,
            MovementOriginRole.GeneralExpenseUser,
            createdByUserId: null);
    }

    public static Movement CreateAdminAdjustment(
        Guid accountId,
        string description,
        decimal signedAmount,
        DateTimeOffset createdAt,
        string? createdByUserId)
    {
        return new Movement(
            accountId,
            description,
            signedAmount,
            shift: null,
            MovementType.AdminAdjustment,
            isAdminAdjustment: true,
            createdAt,
            MovementOriginRole.Administrator,
            createdByUserId);
    }

    internal void AttachToAccount(Account account)
    {
        Account = account;
    }
}
