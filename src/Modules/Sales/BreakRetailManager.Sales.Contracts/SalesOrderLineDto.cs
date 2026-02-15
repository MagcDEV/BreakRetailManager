namespace BreakRetailManager.Sales.Contracts;

public sealed record SalesOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
