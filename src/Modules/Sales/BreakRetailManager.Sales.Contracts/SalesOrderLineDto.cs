namespace BreakRetailManager.Sales.Contracts;

public sealed record SalesOrderLineDto(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
