namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record CreateAdminAdjustmentRequest(
    string Description,
    decimal Amount,
    AdminAdjustmentDirection Direction);
