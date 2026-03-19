namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record CreateMovementRequest(
    string Description,
    decimal Amount,
    string? Shift = null);
