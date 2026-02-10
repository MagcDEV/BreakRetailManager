namespace BreakRetailManager.Sales.Contracts;

public sealed record CreateSalesOrderLineRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
