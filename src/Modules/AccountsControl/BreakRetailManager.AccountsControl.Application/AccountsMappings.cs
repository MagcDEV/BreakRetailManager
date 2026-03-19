using System.Text.Json;
using BreakRetailManager.AccountsControl.Contracts;
using BreakRetailManager.AccountsControl.Domain.Entities;
using ContractAccountType = BreakRetailManager.AccountsControl.Contracts.AccountType;
using ContractMovementOriginRole = BreakRetailManager.AccountsControl.Contracts.MovementOriginRole;
using ContractMovementType = BreakRetailManager.AccountsControl.Contracts.MovementType;
using DomainAccountType = BreakRetailManager.AccountsControl.Domain.AccountType;
using DomainMovementOriginRole = BreakRetailManager.AccountsControl.Domain.MovementOriginRole;
using DomainMovementType = BreakRetailManager.AccountsControl.Domain.MovementType;

namespace BreakRetailManager.AccountsControl.Application;

public static class AccountsMappings
{
    public static AccountOptionDto ToOptionDto(Account account)
    {
        return new AccountOptionDto(
            account.Id,
            account.Name,
            ToContractAccountType(account.Type));
    }

    public static AccountSummaryDto ToSummaryDto(Account account)
    {
        return new AccountSummaryDto(
            account.Id,
            account.Name,
            ToContractAccountType(account.Type),
            account.IsActive,
            decimal.Round(account.Balance, 2, MidpointRounding.AwayFromZero),
            account.MovementCount,
            account.LastActivityAt);
    }

    public static MovementDto ToMovementDto(Movement movement, Account account)
    {
        return new MovementDto(
            movement.Id,
            movement.AccountId,
            account.Name,
            ToContractAccountType(account.Type),
            movement.Description,
            movement.Amount,
            movement.Shift,
            ToContractMovementType(movement.MovementType),
            movement.IsAdminAdjustment,
            movement.CreatedAt,
            ToContractMovementOriginRole(movement.CreatedByRole),
            movement.CreatedByUserId);
    }

    public static MovementDto ToMovementDto(Movement movement)
    {
        if (movement.Account is null)
        {
            throw new InvalidOperationException("Movement account navigation was not loaded.");
        }

        return ToMovementDto(movement, movement.Account);
    }

    public static DomainAccountType ToDomainAccountType(ContractAccountType type)
    {
        return type switch
        {
            ContractAccountType.Employee => DomainAccountType.Employee,
            ContractAccountType.GeneralExpense => DomainAccountType.GeneralExpense,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported account type.")
        };
    }

    public static ContractAccountType ToContractAccountType(DomainAccountType type)
    {
        return type switch
        {
            DomainAccountType.Employee => ContractAccountType.Employee,
            DomainAccountType.GeneralExpense => ContractAccountType.GeneralExpense,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported account type.")
        };
    }

    private static ContractMovementType ToContractMovementType(DomainMovementType movementType)
    {
        return movementType switch
        {
            DomainMovementType.Charge => ContractMovementType.Charge,
            DomainMovementType.Expense => ContractMovementType.Expense,
            DomainMovementType.AdminAdjustment => ContractMovementType.AdminAdjustment,
            _ => throw new ArgumentOutOfRangeException(nameof(movementType), movementType, "Unsupported movement type.")
        };
    }

    private static ContractMovementOriginRole ToContractMovementOriginRole(DomainMovementOriginRole role)
    {
        return role switch
        {
            DomainMovementOriginRole.Employee => ContractMovementOriginRole.Employee,
            DomainMovementOriginRole.GeneralExpenseUser => ContractMovementOriginRole.GeneralExpenseUser,
            DomainMovementOriginRole.Administrator => ContractMovementOriginRole.Administrator,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported movement role.")
        };
    }

    public static string ToAuditPayload(object payload)
    {
        return JsonSerializer.Serialize(payload);
    }
}
