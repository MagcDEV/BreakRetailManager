namespace BreakRetailManager.Sales.Contracts;

public sealed record CreateSalesOrderRequest(
    IReadOnlyList<CreateSalesOrderLineRequest> Lines,
    Guid LocationId,
    PaymentMethod PaymentMethod = PaymentMethod.Cash);
