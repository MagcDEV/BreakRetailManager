namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record PublicSummaryDto(
    decimal TotalOutstandingBalance,
    decimal TotalGeneralExpenseBalance,
    decimal TotalCollected);
