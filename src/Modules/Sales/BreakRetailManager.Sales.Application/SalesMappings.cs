using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Domain.Entities;

namespace BreakRetailManager.Sales.Application;

public static class SalesMappings
{
    public static SalesOrderDto ToDto(SalesOrder order)
    {
        return new SalesOrderDto(
            order.Id,
            order.Number,
            order.CreatedAt,
            order.Total,
            order.Lines
                .Select(line => new SalesOrderLineDto(line.Id, line.ProductName, line.Quantity, line.UnitPrice))
                .ToList(),
            order.PaymentMethod,
            order.LocationId,
            order.Cae,
            order.CaeExpirationDate,
            order.InvoiceNumber,
            order.PointOfSale);
    }

    public static SalesOrder FromRequest(CreateSalesOrderRequest request)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(request));
        }

        var order = new SalesOrder(
            $"SO-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            DateTimeOffset.UtcNow);

        order.SetPaymentMethod(request.PaymentMethod);
        order.SetLocation(request.LocationId);

        foreach (var line in request.Lines)
        {
            order.AddLine(line.ProductName, line.Quantity, line.UnitPrice);
        }

        return order;
    }
}
