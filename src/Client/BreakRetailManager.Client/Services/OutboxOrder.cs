using BreakRetailManager.Sales.Contracts;

namespace BreakRetailManager.Client.Services;

public sealed record OutboxOrder(Guid Id, CreateSalesOrderRequest Request, DateTimeOffset CreatedAt);
