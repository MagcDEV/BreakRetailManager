namespace BreakRetailManager.AccountsControl.Domain.Entities;

public sealed class Account
{
    private readonly List<Movement> _movements = [];

    private Account()
    {
    }

    public Account(string name, AccountType type, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Type = type;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        SetName(name);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public AccountType Type { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<Movement> Movements => _movements;

    public decimal Balance => _movements.Sum(movement => movement.Amount);

    public int MovementCount => _movements.Count;

    public DateTimeOffset? LastActivityAt => _movements.Count == 0
        ? null
        : _movements.Max(movement => movement.CreatedAt);

    public void AddMovement(Movement movement)
    {
        ArgumentNullException.ThrowIfNull(movement);

        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive accounts cannot receive new movements.");
        }

        if (movement.AccountId != Id)
        {
            throw new ArgumentException("Movement does not belong to this account.", nameof(movement));
        }

        movement.AttachToAccount(this);
        _movements.Add(movement);
    }

    public void Deactivate(DateTimeOffset timestamp)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeletedAt = timestamp;
        UpdatedAt = timestamp;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Account name is required.", nameof(name));
        }

        Name = name.Trim();
        NormalizedName = NormalizeName(Name);
    }

    public static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToUpperInvariant();
    }
}
