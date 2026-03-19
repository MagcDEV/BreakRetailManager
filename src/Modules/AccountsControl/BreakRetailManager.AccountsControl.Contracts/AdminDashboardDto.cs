namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record AdminDashboardDto(
    decimal TotalOutstandingBalance,
    decimal TotalGeneralExpenseBalance,
    decimal TotalCollected,
    IReadOnlyList<AccountSummaryDto> EmployeeAccounts,
    IReadOnlyList<AccountSummaryDto> ExpenseAccounts);
