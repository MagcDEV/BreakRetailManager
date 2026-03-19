namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record AccountSummaryDto(
    Guid Id,
    string Name,
    AccountType Type,
    bool IsActive,
    decimal Balance,
    int MovementCount,
    DateTimeOffset? LastActivityAt);
