namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record MovementDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    AccountType AccountType,
    string Description,
    decimal Amount,
    string? Shift,
    MovementType MovementType,
    bool IsAdminAdjustment,
    DateTimeOffset CreatedAt,
    MovementOriginRole CreatedByRole,
    string? CreatedByUserId);
