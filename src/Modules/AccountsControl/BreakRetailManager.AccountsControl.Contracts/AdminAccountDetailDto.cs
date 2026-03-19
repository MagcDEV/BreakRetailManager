namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record AdminAccountDetailDto(
    AccountSummaryDto Account,
    IReadOnlyList<MovementDto> Movements);
