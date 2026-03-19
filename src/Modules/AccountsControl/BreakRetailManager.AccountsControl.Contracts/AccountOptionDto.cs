namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record AccountOptionDto(
    Guid Id,
    string Name,
    AccountType Type);
