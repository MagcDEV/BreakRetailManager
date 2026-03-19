namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record CreateAccountRequest(
    string Name,
    AccountType Type);
